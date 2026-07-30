import http from 'k6/http';
import { Counter } from 'k6/metrics';
import { sleep } from 'k6';
import { BASE_URL, DEFAULT_DURATION, checkExpectedStatus, jsonHeaders, scenarioRate } from './common.js';

const blocked = new Counter('auth_blocked_429');
const failed = new Counter('auth_failed_401');
const unexpected = new Counter('auth_unexpected_status');
const requireBlock = (__ENV.REQUIRE_BLOCK || 'false').toLowerCase() === 'true';

export const options = {
  scenarios: {
    login_bruteforce: {
      executor: 'constant-arrival-rate',
      rate: scenarioRate(60),
      timeUnit: '1m',
      duration: DEFAULT_DURATION,
      preAllocatedVUs: Number(__ENV.PREALLOCATED_VUS || 20),
      maxVUs: Number(__ENV.MAX_VUS || 80)
    }
  },
  thresholds: Object.assign({
    'http_req_duration{endpoint:auth_login}': ['p(95)<2000']
  }, requireBlock ? {
    auth_blocked_429: ['count>0']
  } : {})
};

export default function () {
  const email = __ENV.TARGET_EMAIL || 'security.simulation@owen.local';
  const password = __ENV.BAD_PASSWORD || `bad-${__VU}-${__ITER}`;

  const res = http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({ email, password }),
    { headers: jsonHeaders(), tags: { endpoint: 'auth_login' } }
  );

  checkExpectedStatus(res, 'login brute force', [401, 429]);

  if (res.status === 429) blocked.add(1);
  else if (res.status === 401) failed.add(1);
  else unexpected.add(1);

  sleep(Number(__ENV.SLEEP_SECONDS || 0.1));
}

