import http from 'k6/http';
import { Counter } from 'k6/metrics';
import { sleep } from 'k6';
import { BASE_URL, DEFAULT_DURATION, authHeaders, checkExpectedStatus, scenarioRate } from './common.js';

const rejected = new Counter('reports_rejected');
const generated = new Counter('reports_generated');

const idCotizacion = __ENV.REPORT_COTIZACION_ID || __ENV.COTIZACION_ID || '1';
const reportPath = __ENV.REPORT_PATH || `/api/Reporte/subastas/${idCotizacion}/auditoria/pdf`;

export const options = {
  scenarios: {
    reports_pressure: {
      executor: 'constant-arrival-rate',
      rate: scenarioRate(18),
      timeUnit: '1m',
      duration: DEFAULT_DURATION,
      preAllocatedVUs: Number(__ENV.PREALLOCATED_VUS || 10),
      maxVUs: Number(__ENV.MAX_VUS || 40)
    }
  },
  thresholds: {
    'http_req_duration{endpoint:report_pdf}': ['p(95)<10000']
  }
};

export default function () {
  const res = http.get(`${BASE_URL}${reportPath}`, {
    headers: authHeaders(),
    tags: { endpoint: 'report_pdf' },
    timeout: __ENV.TIMEOUT || '30s'
  });

  checkExpectedStatus(res, 'reporte pesado', [200, 401, 403, 404, 429, 503]);

  if (res.status === 200) generated.add(1);
  if ([401, 403, 429, 503].includes(res.status)) rejected.add(1);

  sleep(Number(__ENV.SLEEP_SECONDS || 0.1));
}

