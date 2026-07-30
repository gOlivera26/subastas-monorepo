import http from 'k6/http';
import { Counter } from 'k6/metrics';
import { sleep } from 'k6';
import { BASE_URL, DEFAULT_DURATION, checkExpectedStatus, parseJson, responseData, scenarioRate } from './common.js';

const blocked = new Counter('public_blocked_429');
const degraded = new Counter('public_degraded_5xx');

export const options = {
  scenarios: {
    public_scraping: {
      executor: 'constant-arrival-rate',
      rate: scenarioRate(240),
      timeUnit: '1m',
      duration: DEFAULT_DURATION,
      preAllocatedVUs: Number(__ENV.PREALLOCATED_VUS || 30),
      maxVUs: Number(__ENV.MAX_VUS || 120)
    }
  },
  thresholds: {
    'http_req_duration{endpoint:public_auction_list}': ['p(95)<1200'],
    'http_req_duration{endpoint:public_auction_detail}': ['p(95)<1500']
  }
};

export default function () {
  const list = http.get(`${BASE_URL}/api/CotizacionPublica/activas`, {
    tags: { endpoint: 'public_auction_list' }
  });

  checkExpectedStatus(list, 'subastas públicas activas', [200, 429, 503]);
  if (list.status === 429) blocked.add(1);
  if (list.status >= 500) degraded.add(1);

  const data = responseData(parseJson(list));
  if (list.status === 200 && Array.isArray(data) && data.length > 0) {
    const selected = data[(__ITER + __VU) % data.length];
    const id = selected.idCotizacion ?? selected.IdCotizacion ?? selected.id ?? selected.Id;

    if (id) {
      const detail = http.get(`${BASE_URL}/api/CotizacionPublica/${id}`, {
        tags: { endpoint: 'public_auction_detail' }
      });
      checkExpectedStatus(detail, 'detalle público de subasta', [200, 404, 429, 503]);
      if (detail.status === 429) blocked.add(1);
      if (detail.status >= 500) degraded.add(1);
    }
  }

  sleep(Number(__ENV.SLEEP_SECONDS || 0.05));
}

