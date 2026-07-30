import http from 'k6/http';
import { Counter } from 'k6/metrics';
import { sleep } from 'k6';
import { BASE_URL, DEFAULT_DURATION, bearerHeaders, checkExpectedStatus, requireWriteSimulation, scenarioRate } from './common.js';

requireWriteSimulation();

const rejected = new Counter('uploads_invalid_rejected');
const accepted = new Counter('uploads_invalid_accepted');

const uploadUrl = __ENV.UPLOAD_URL;
if (!uploadUrl) {
  throw new Error('Falta UPLOAD_URL. Ej: /api/Cotizacion/1/Documento o /api/Garantia');
}

const fileField = __ENV.FILE_FIELD || 'archivo';
const fileName = __ENV.FILE_NAME || 'payload.pdf';
const mimeType = __ENV.MIME_TYPE || 'application/pdf';
const extraFields = __ENV.EXTRA_FIELDS ? JSON.parse(__ENV.EXTRA_FIELDS) : {};

export const options = {
  scenarios: {
    invalid_uploads: {
      executor: 'constant-arrival-rate',
      rate: scenarioRate(30),
      timeUnit: '1m',
      duration: DEFAULT_DURATION,
      preAllocatedVUs: Number(__ENV.PREALLOCATED_VUS || 10),
      maxVUs: Number(__ENV.MAX_VUS || 40)
    }
  },
  thresholds: {
    'http_req_duration{endpoint:invalid_upload}': ['p(95)<3000']
  }
};

export default function () {
  const data = Object.assign({}, extraFields);
  data[fileField] = http.file('MZ fake binary payload, not a real PDF', fileName, mimeType);

  const res = http.post(`${BASE_URL}${uploadUrl}`, data, {
    headers: bearerHeaders(),
    tags: { endpoint: 'invalid_upload' },
    timeout: __ENV.TIMEOUT || '15s'
  });

  checkExpectedStatus(res, 'upload inválido', [400, 401, 403, 404, 413, 415, 422, 429, 503]);

  if ([400, 413, 415, 422, 429, 503].includes(res.status)) rejected.add(1);
  if (res.status >= 200 && res.status < 300) accepted.add(1);

  sleep(Number(__ENV.SLEEP_SECONDS || 0.1));
}
