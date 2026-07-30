import http from 'k6/http';
import { Counter } from 'k6/metrics';
import { sleep } from 'k6';
import { BASE_URL, DEFAULT_DURATION, authHeaders, checkExpectedStatus, requireWriteSimulation, scenarioRate } from './common.js';

requireWriteSimulation();

const accepted = new Counter('offers_accepted');
const rejected = new Counter('offers_rejected');

const cotizacionId = __ENV.COTIZACION_ID;
if (!cotizacionId) {
  throw new Error('Falta COTIZACION_ID para simular ofertas.');
}

const body = __ENV.OFFER_BODY
  ? JSON.parse(__ENV.OFFER_BODY)
  : [{ idCotizacionDetalle: null, idRenglon: 1, monto: 1000, idMonedaOferta: 1, cantidad: 1 }];

export const options = {
  scenarios: {
    offers_concurrency: {
      executor: 'constant-arrival-rate',
      rate: scenarioRate(60),
      timeUnit: '1m',
      duration: DEFAULT_DURATION,
      preAllocatedVUs: Number(__ENV.PREALLOCATED_VUS || 20),
      maxVUs: Number(__ENV.MAX_VUS || 100)
    }
  },
  thresholds: {
    'http_req_duration{endpoint:offer_batch}': ['p(95)<1500']
  }
};

export default function () {
  const res = http.post(
    `${BASE_URL}/api/OfertaSubasta/${cotizacionId}/Batch`,
    JSON.stringify(body),
    { headers: authHeaders(), tags: { endpoint: 'offer_batch' } }
  );

  checkExpectedStatus(res, 'oferta concurrente', [200, 400, 401, 403, 404, 409, 422, 429, 503]);

  if (res.status === 200) accepted.add(1);
  else rejected.add(1);

  sleep(Number(__ENV.SLEEP_SECONDS || 0.05));
}

