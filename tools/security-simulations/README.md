# Simulaciones de seguridad OWEN / PortalSubastas

Scripts k6 para simular abuso realista contra **local/staging controlado**. No son tests felices: buscan generar presión y observar si los límites, logs, métricas y correlation IDs se comportan bien.

> No ejecutar contra producción sin ventana aprobada, WAF/CDN configurado y monitoreo activo.

## Requisitos

- k6 instalado: <https://k6.io/docs/get-started/installation/>
- Gateway levantado, por defecto en `http://localhost:5000`.
- Para endpoints autenticados: `TOKEN` o credenciales válidas según el script.

## Variables comunes

| Variable | Default | Uso |
|---|---:|---|
| `BASE_URL` | `http://localhost:5000` | URL del gateway. |
| `DURATION` | `3m` | Duración de la simulación. |
| `RATE` | depende del script | Llegadas por minuto. |
| `PREALLOCATED_VUS` / `MAX_VUS` | depende del script | Capacidad k6. |
| `TOKEN` | vacío | JWT para endpoints protegidos. |
| `REQUIRE_BLOCK` | `false` | En login, falla si no aparece al menos un 429. |
| `ALLOW_WRITE_SIMULATION` | `false` | Requerido para ofertas/uploads. |

## Escenarios

### 1) Brute force de login

```bash
k6 run --env BASE_URL=http://localhost:5000 --env TARGET_EMAIL=admin@innovanow.net --env RATE=120 --env REQUIRE_BLOCK=true tools/security-simulations/login-bruteforce.js
```

Esperado: mezcla de `401` y luego `429`; métricas `auth.login.failed`, `auth.lockout`, logs `SECURITY_AUTH_FAILURE` / `SECURITY_AUTH_LOCKOUT`.

### 2) Scraping público de subastas activas

```bash
k6 run --env BASE_URL=http://localhost:5000 --env RATE=600 tools/security-simulations/public-scraping.js
```

Esperado: `200` bajo carga moderada; `429/503` cuando se excedan límites; correlation ID en respuestas.

### 3) Presión de reportes PDF

```bash
k6 run --env BASE_URL=http://localhost:5000 --env TOKEN=ey... --env REPORT_COTIZACION_ID=1 --env RATE=30 tools/security-simulations/reports-pressure.js
```

Esperado: reportes generados controladamente o rechazados con `429/503` si se supera la política `reports-heavy`.

### 4) Ofertas concurrentes

```bash
k6 run \
  --env BASE_URL=http://localhost:5000 \
  --env TOKEN=ey... \
  --env ALLOW_WRITE_SIMULATION=true \
  --env COTIZACION_ID=1 \
  --env OFFER_BODY='[{"idCotizacionDetalle":null,"idRenglon":1,"monto":1000,"idMonedaOferta":1,"cantidad":1}]' \
  tools/security-simulations/offers-concurrency.js
```

Esperado: ofertas válidas aceptadas sin degradar la sala; abuso limitado con `429` sin romper la operatoria legítima.

### 5) Uploads inválidos

```bash
k6 run \
  --env BASE_URL=http://localhost:5000 \
  --env TOKEN=ey... \
  --env ALLOW_WRITE_SIMULATION=true \
  --env UPLOAD_URL=/api/Cotizacion/1/Documento \
  --env FILE_FIELD=archivo \
  tools/security-simulations/uploads-invalid.js
```

Esperado: rechazo `400/413/415/422/429`, logs de upload sospechoso y sin persistir binarios inválidos.

## Qué mirar durante la simulación

- Gateway: `security.rate_limit.rejected`, `security.kill_switch.rejected`.
- Identity: `auth.login.failed`, `auth.lockout`.
- Códigos HTTP: picos de `401/403/429/503`.
- `X-Correlation-ID` presente y rastreable de gateway a microservicio.
- Latencia p95/p99 y errores 5xx.
- Audit worker / logs: eventos `SECURITY_*`.

## Criterio de corte

Abortar si:

- 5xx sostenido en endpoints esenciales.
- DB/Rabbit/CPU/Memoria superan umbrales operativos.
- Una simulación de escritura empieza a afectar subastas reales.
- Se observa fuga de stacktrace o dato sensible en respuestas.

