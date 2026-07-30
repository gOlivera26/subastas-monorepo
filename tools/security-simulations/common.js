import http from 'k6/http';
import { check } from 'k6';

export const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5000').replace(/\/$/, '');
export const DEFAULT_DURATION = __ENV.DURATION || '3m';

export function jsonHeaders(extra = {}) {
  return Object.assign({
    'Content-Type': 'application/json',
    'X-Security-Simulation': __ENV.SIMULATION_NAME || 'owen-security-sim'
  }, extra);
}

export function authHeaders(extra = {}) {
  const token = __ENV.TOKEN || '';
  return jsonHeaders(Object.assign(token ? { Authorization: `Bearer ${token}` } : {}, extra));
}

export function bearerHeaders(extra = {}) {
  const token = __ENV.TOKEN || '';
  return Object.assign({
    'X-Security-Simulation': __ENV.SIMULATION_NAME || 'owen-security-sim'
  }, token ? { Authorization: `Bearer ${token}` } : {}, extra);
}

export function parseJson(res) {
  try {
    return res.json();
  } catch (_) {
    return null;
  }
}

export function responseData(payload) {
  if (!payload) return null;
  return payload.data ?? payload.Data ?? payload.result ?? payload.Result ?? null;
}

export function checkExpectedStatus(res, name, allowedStatuses) {
  return check(res, {
    [`${name}: status esperado`]: (r) => allowedStatuses.includes(r.status),
    [`${name}: correlation id expuesto`]: (r) => Boolean(r.headers['X-Correlation-ID'] || r.headers['x-correlation-id'])
  });
}

export function login(email = __ENV.AUTH_EMAIL, password = __ENV.AUTH_PASSWORD) {
  if (!email || !password) {
    return null;
  }

  return http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({ email, password }),
    { headers: jsonHeaders(), tags: { endpoint: 'auth_login' } }
  );
}

export function requireWriteSimulation() {
  if ((__ENV.ALLOW_WRITE_SIMULATION || 'false').toLowerCase() !== 'true') {
    throw new Error('Simulación de escritura bloqueada. Seteá ALLOW_WRITE_SIMULATION=true sólo contra local/staging controlado.');
  }
}

export function scenarioRate(defaultRate) {
  return Number(__ENV.RATE || defaultRate);
}
