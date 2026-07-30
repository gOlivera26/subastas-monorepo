# Operación, SLO y pruebas de seguridad — OWEN / PortalSubastas

## SLO inicial recomendado

| Área | Objetivo v1 | Medición |
| --- | --- | --- |
| Gateway uptime | 99.5% mensual | healthcheck + 5xx sostenidos |
| Login p95 | < 800 ms | OpenTelemetry / gateway logs |
| Subasta/oferta p95 | < 500 ms | `OfertaSubasta` + SignalR |
| Reporte PDF p95 | < 15 s | `report.generated` / gateway duration |
| Error budget | 0.5% mensual | 5xx + incidentes SEV |

## Métricas de seguridad a exponer

- `security.rate_limit.rejected`
- `security.kill_switch.rejected`
- `auth.login.failed`
- `auth.lockout`
- `auth.password_reset.requested`
- `upload.rejected`
- `report.generated`
- `report.rejected`
- `authorization.denied`

## Dashboards mínimos

1. Salud por microservicio: Gateway, Identity, Licitaciones, Providers, Reporting, Audit.
2. Latencia p95/p99 por endpoint crítico.
3. 401/403/429/5xx por minuto.
4. Gateway 502/504 por cluster YARP.
5. Top IPs rechazadas por rate limit.
6. Reportes PDF generados/fallidos.
7. Uploads rechazados por tipo/tamaño/magic bytes.
8. Subastas activas afectadas por incidentes.

## Pruebas manuales rápidas

| Prueba | Esperado |
| --- | --- |
| 6 logins fallidos mismo email/IP | 401 inicial y luego 429 con mensaje amable |
| `DisablePublicRegistration=true` + POST register | 503 con `correlationId` |
| PDF con `DisablePdfReports=true` | 503 sin golpear Reporting |
| Upload `.exe` renombrado | 400 por extensión/MIME/magic bytes |
| Archivo > 20 MB | 400/413 según corte de servidor |
| Endpoint con JWT débil en Production | API no arranca |
| CORS vacío en Production | API no arranca |
| Error inesperado en Production | 500 sin stacktrace y con `correlationId` |

## Simulaciones de abuso

Las simulaciones k6 versionadas viven en `tools/security-simulations/` y cubren:

- brute force de login;
- scraping público de subastas;
- presión de reportes PDF;
- ofertas concurrentes;
- uploads inválidos.

Se ejecutan contra local/staging controlado. Ofertas y uploads requieren `ALLOW_WRITE_SIMULATION=true` para evitar escrituras accidentales.

Ejemplo:

```bash
k6 run --env BASE_URL=http://localhost:5000 --env TARGET_EMAIL=admin@innovanow.net --env RATE=120 --env REQUIRE_BLOCK=true tools/security-simulations/login-bruteforce.js
```

Durante la simulación revisar:

- presencia de `X-Correlation-ID`;
- spikes de `401/403/429/503`;
- counters `security.*` y `auth.*`;
- eventos `SECURITY_*` en auditoría/logs;
- p95/p99 y 5xx sostenidos.

## Herramientas recomendadas

- **OWASP ZAP Baseline**: DAST contra ambiente test.
- **k6/JMeter**: carga y abuso de login/reportes/ofertas.
- **Gitleaks**: detección de secretos en repo/CI.
- **Trivy**: vulnerabilidades en filesystem e imágenes Docker.
- **Dependabot/Renovate**: PRs de dependencias vulnerables.
- **Cloudflare Security Analytics**: WAF, bot, rates e IP reputation.
- **Lighthouse/axe**: accesibilidad frontend y WCAG.

## Criterio de salida para fase 1/2

- Gateway compila y responde 429/503 con `correlationId`.
- Identity, Licitaciones, Providers y Reporting compilan.
- Uploads críticos rechazan archivos inválidos.
- CI ejecuta build base y controles no bloqueantes de supply chain.
- Runbooks y baseline quedan versionados para soporte/operación.
- `/health/ready` responde 200/503 según dependencias configuradas.
- Simulaciones k6 quedan listas para staging/local antes de exponer el sistema.
