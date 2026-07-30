# Security baseline — OWEN / PortalSubastas

## Objetivo
Dejar una base operativa de seguridad por capas para el monorepo, alineada con NIST CSF 2.0 y OWASP API Security. Esta baseline separa lo ya protegido en v1 de los controles que requieren infraestructura, esquema nuevo o decisiones de negocio.

## Inventario funcional

| Área | Microservicio / punto | Clasificación | Riesgo principal | Controles v1 |
| --- | --- | --- | --- | --- |
| Gateway público | `PortalSubastas.Gateway` / YARP | Público / Interno | abuso, scraping, DDoS app-layer | rate limits por ruta, headers, correlation id, kill switches |
| Identidad | `PortalSubastas.Identity.API` / `/api/Auth/*` | Sensible | brute force, enumeración, tokens débiles | lockout progresivo en memoria, errores homogéneos, JWT/CORS validation |
| Administración | `Identity.API` / usuarios, roles, páginas | Sensible | escalamiento de privilegios | rate limit admin, JWT validation, auditoría existente |
| Proveedores | `Providers.API` / proveedor, rubros, domicilios | Interno / Sensible | carga masiva abusiva, archivos inválidos | validación CSV/documentos, rate limit write-api/upload |
| Licitaciones | `Licitaciones.API` / cotización, reservas, ofertas | Interno / Sensible | BOLA/IDOR, abuso de ofertas | rate limit por ofertas, JWT/CORS validation, errores sin stacktrace prod |
| Documentos / garantías | `Licitaciones.API` / garantías y documentos | Sensible | malware, MIME spoofing, path traversal | límite 20 MB, extensión, content-type, magic bytes, filename saneado |
| Reportería | `Reporting.API` / `/api/Reporte/*` | Interno / Sensible | consumo alto CPU/memoria, PDF abuse | rate limit de baja frecuencia, kill switch PDF, errores con correlation id |
| Público | `CotizacionPublica` / subastas activas | Público | scraping y enumeración | rate limit public-read, headers y correlation id |
| SignalR | `/signalr/subastas` | Público / Autenticado | conexiones excesivas | rate limit SignalR en gateway |

## Clasificación de datos

| Clase | Datos | Reglas mínimas |
| --- | --- | --- |
| Público | subastas activas, detalle público, estado público | cache/consulta permitida, sin datos internos de proveedor |
| Interno | notas, cotizaciones, reservas, rubros, reportes operativos | requiere JWT y autorización por módulo/página/contexto |
| Sensible | usuarios, roles, documentos, garantías, tokens, logs, credenciales | no exponer stacktrace, no URLs públicas, auditoría, controles extra |
| Crítico | auth, reset, ofertas, reportes PDF, uploads, administración, seguridad | rate limit estricto, registro de evento, kill switch cuando aplique |

## Endpoints críticos

- Auth: `POST /api/Auth/login`, `register`, `solicitar-reset`, `reset-password`, `confirmar-email`, `reenviar-codigo`.
- Ofertas: `/api/OfertaSubasta/**`.
- Uploads: `/api/Garantia/**`, `/api/Cotizacion/{id}/Documento/**`, `/api/Cotizacion/{id}/DocumentoItem/**`, constancia AFIP, bulk upload de rubros.
- Reporting: `/api/Reporte/**/pdf` y exportaciones.
- Administración/seguridad: `/api/User/**`, `/api/Role/**`, páginas, roles, organizaciones y clasificadores base.
- Cotizaciones: `/api/Cotizacion/{id}/**`, ganadores, invitaciones, especificaciones.

## Controles implementados en v1

1. Rate limits configurables en Gateway (`Security:RateLimiting`) por costo funcional.
2. Kill switches de Gateway (`Security:KillSwitches`) para registro público, PDF, sólo lectura y emergencia de subasta.
3. Headers HTTP de seguridad y correlation id transversal (`X-Correlation-ID`).
4. Validación de producción para CORS, proxies confiables y JWT secret fuerte.
5. Login con lockout progresivo por email, IP e IP+email usando `IMemoryCache`.
6. Errores de auth más homogéneos para reducir enumeración.
7. Middlewares de excepción sin stacktrace en Production y con `correlationId` para soporte.
8. Validación de uploads por tamaño, extensión, MIME declarado y magic bytes.
9. `/health/ready` en microservicios con checks mínimos de DB/config dependiente.
10. Auditoría enriquecida con IP real, user-agent, path, método y `X-Correlation-ID`.
11. Métricas OTEL iniciales para rate limits, kill switches, login fallido y lockout.
12. CI ampliado con build, test base, auditoría de paquetes, Gitleaks, Trivy y ZAP opcional.
13. Simulaciones k6 versionadas para abuso de login, scraping público, reportes, ofertas y uploads.

## Controles pendientes para fases posteriores

| Control | Motivo de diferimiento |
| --- | --- |
| MFA/TOTP para admins | requiere flujo UX, secreto TOTP y persistencia por usuario |
| Refresh token rotation / session registry | requiere modelo de sesión y migración de BD |
| Rate limiting distribuido | requiere Redis u otro store compartido si hay múltiples gateways |
| Antivirus/antimalware | requiere ClamAV/servicio externo y pipeline de cuarentena |
| URLs firmadas/storage privado integral | requiere revisar `FileStorageService` de todos los módulos y política R2 |
| Dashboards/alertas completas | métricas base listas; requiere backend de métricas y tableros operativos |
| Backups/restore drills | requiere infraestructura y agenda operativa |

## Checklist OWASP mínimo para nuevas features

- ¿El endpoint tiene auth cuando corresponde?
- ¿Los `{id}` se validan contra contexto del usuario? Evitar BOLA/IDOR.
- ¿El payload tiene límites de tamaño y validación semántica?
- ¿Los errores no filtran stacktrace ni datos internos en Production?
- ¿Hay auditoría para acciones críticas?
- ¿Hay rate limit o control por costo si el endpoint es caro?
- ¿El endpoint aparece en el inventario y en el mapa de permisos si aplica?
- ¿Se probó 401/403/429 y correlation id?
