# Runbooks de respuesta ante incidentes — OWEN / PortalSubastas

## Convenciones

- **SEV1**: caída general, exposición de datos o compromiso de credenciales/secrets.
- **SEV2**: degradación fuerte, abuso activo bloqueado parcialmente o riesgo de datos acotado.
- **SEV3**: intento bloqueado, alerta menor o bug de seguridad sin explotación confirmada.
- Registrar siempre: hora, responsable, `correlationId`, IP, usuario, endpoint, decisión tomada y evidencia.

## 1. Brute force / ataque de login

1. Confirmar spikes de `401`, `429`, `SECURITY_AUTH_FAILURE` y `SECURITY_AUTH_LOCKOUT`.
2. Identificar top IPs y emails atacados.
3. Ajustar temporalmente `Security:RateLimiting:AuthStrict` si el gateway queda saturado.
4. Si hay abuso masivo, bloquear IP/rango/país en Cloudflare/WAF.
5. Revisar cuentas con login exitoso cerca del ataque.
6. Si hay sospecha de compromiso, forzar reset y revocar refresh tokens cuando exista registry.
7. Generar postmortem si SEV1/SEV2.

## 2. DDoS / scraping público

1. Confirmar picos en `public-read`, 429 y CPU/memoria del gateway.
2. Activar reglas Cloudflare/WAF: rate limit, bot fight/challenge, bloqueo por ASN/país si corresponde.
3. Bajar temporalmente `Security:RateLimiting:PublicRead`.
4. Si afecta operación crítica, activar `AuctionEmergencyMode` para sostener ofertas/consultas esenciales.
5. Registrar endpoints más atacados y evidencia.

## 3. Abuso de reportes PDF

1. Verificar latencia p95/p99 de Reporting y errores 502/504 en Gateway.
2. Bajar `Security:RateLimiting:ReportsHeavy` o activar `DisablePdfReports`.
3. Confirmar que el resto del sistema continúa operativo.
4. Revisar `correlationId` de solicitudes pesadas y usuario solicitante.
5. Rehabilitar gradualmente cuando baje la presión.

## 4. Secret/JWT filtrado

1. Declarar SEV1.
2. Rotar `Jwt:SecretKey`, connection strings, R2, RabbitMQ y API keys afectadas.
3. Invalidar sesiones/refresh tokens cuando exista registry; mientras tanto reducir vida del JWT y forzar login.
4. Revisar logs de acceso con el rango temporal de exposición.
5. Ejecutar Gitleaks y revisar commits/tags afectados.
6. Postmortem obligatorio.

## 5. Admin comprometido

1. Desactivar el usuario o cambiar rol desde SUPERADMIN confiable.
2. Rotar password y MFA cuando esté implementado.
3. Revisar acciones críticas: roles, páginas, subastas, ganadores, documentos, reportes.
4. Exportar bitácora/auditoría con `correlationId`.
5. Si hubo modificación de datos, activar modo sólo lectura hasta terminar análisis.

## 6. Upload/documento sospechoso

1. Localizar archivo por hash/nombre/subasta/proveedor.
2. Bloquear descarga pública o remover acceso firmado.
3. Analizar con antivirus/servicio externo cuando esté disponible.
4. Revisar usuario/IP que subió el archivo y otros documentos del mismo origen.
5. Mantener evidencia; no borrar antes de preservar hash y metadata.

## 7. Modo sólo lectura

1. Activar `Security:KillSwitches:ReadOnlyMode=true` en Gateway.
2. Validar que login y consultas críticas funcionen.
3. Comunicar a soporte/operación.
4. Resolver incidente, monitorear 5xx/429 y desactivar el switch.

## 8. Emergencia de subasta

1. Activar `Security:KillSwitches:AuctionEmergencyMode=true`.
2. Verificar que `/api/OfertaSubasta/**`, consultas de cotización y SignalR sigan funcionando.
3. Bloquear administración/reportes/uploads no esenciales hasta estabilizar.
4. Registrar subastas activas afectadas y ventanas horarias.
