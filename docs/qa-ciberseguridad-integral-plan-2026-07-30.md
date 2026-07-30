# Plan de QA integral, ciberseguridad y persistencia — OWEN / PortalSubastas

Fecha: 2026-07-30  
Proyecto: OWEN / PortalSubastas  
Objetivo: validar robustez real del sistema desde el login hasta el circuito completo de subasta, incluyendo ABMs, persistencia, auditoría, eventos y resistencia controlada.

## Principios del recorrido

- El recorrido inicia desde la pantalla de login.
- Los datos creados deben ser verosímiles y propios de negocio.
- No se usan prefijos `QA`, `TEST`, `AUTO`, ni nombres artificiales visibles.
- El administrador configura y gestiona, pero no oferta.
- Las ofertas deben ser realizadas con usuarios proveedores distintos.
- La auditoría se correlaciona por pantalla, acción, tabla, operación, usuario, fecha/hora y correlation id cuando aplique.
- RabbitMQ/eventos se revisan como complemento de auditoría y persistencia.
- Las pruebas de resistencia son controladas: buscan validar 401/403/429/5xx, no tumbar el entorno.

## Datos de negocio autorizados

### Proveedores

Se crearán, si no existen, al menos dos proveedores bajo lógica Olivera:

| Proveedor | Uso |
|---|---|
| Olivera Suministros | Representante/proveedor participante 1 |
| Olivera Servicios Generales | Representante/proveedor participante 2 |

Los emails fueron provistos por el usuario en la conversación actual y se usarán sólo para ejecución del circuito. No se listan en este documento para evitar exposición innecesaria de datos personales.

## Implementación de robustez previa al recorrido

Antes de ejecutar el circuito completo se robustecerá el gateway:

1. Matriz completa de rate limits por superficie funcional.
2. Diferenciación por método/riesgo cuando aplique.
3. Política global para todo el gateway.
4. Políticas específicas para:
   - auth/login/register/reset,
   - endpoints públicos,
   - admin/seguridad,
   - ABMs,
   - uploads,
   - reportes,
   - ofertas,
   - SignalR,
   - rutas fallback.
5. Trazabilidad persistente para rechazos `429` del gateway mediante evento de sistema hacia RabbitMQ/Audit Worker.
6. Correlation ID consistente para relacionar gateway → microservicio → auditoría.

## Matriz de pruebas funcionales

| Área | Prueba |
|---|---|
| Login | login válido, inválido, lockout, error homogéneo |
| Usuarios | alta/autorización si el circuito lo requiere |
| Proveedores | alta/edición/representantes/rubros/invitación |
| Clasificadores | ABMs principales desde cero, con baja lógica si corresponde |
| Nota de pedido | creación de nota válida para subasta |
| Subasta | creación/configuración/documentación/invitaciones/envío |
| Sala proveedor | login proveedor 1, oferta; login proveedor 2, oferta |
| Reportes | generación con spinner/espera, rate limit controlado |
| Documentos | opcionalidad/obligatoriedad, uploads válidos e inválidos |
| Auditoría | t_auditoria_datos y t_logs_eventos por movimiento |
| RabbitMQ | colas/eventos relevantes durante acciones críticas |

## Matriz de pruebas de ciberseguridad

| Superficie | Prueba esperada |
|---|---|
| Auth | fuerza bruta controlada debe terminar en 429/lockout |
| Auth | email existente vs inexistente no debe revelar diferencia al usuario |
| API sin token | endpoints privados deben devolver 401 |
| BOLA/IDOR | proveedor A no debe acceder/ofertar como proveedor B |
| Ofertas | límites por proveedor/subasta/usuario/IP sin bloquear uso normal |
| Reportes | requests concurrentes deben limitarse con 429 y Retry-After |
| Uploads | extensión/MIME inválidos deben rechazarse sin 500 |
| Admin | usuario no admin no debe acceder a ABMs/seguridad |
| Gateway | todo 429 debe generar evento auditable |
| Persistencia | toda acción crítica debe tener registro de auditoría/evento |

## Resultados

### 2026-07-30 — Robustez gateway y baseline de seguridad

| Ítem | Resultado |
|---|---|
| Build gateway | OK |
| Docker backend | Gateway, Identity, Providers, Licitaciones, Reporting, Audit Worker y RabbitMQ levantados |
| Rate limits | Implementada matriz por superficie: auth, público, ABM, admin, uploads, reportes, ofertas, SignalR y fallback |
| Persistencia 429 | OK: los rechazos del gateway publican evento `SECURITY_RATE_LIMIT` vía RabbitMQ/Audit Worker |
| Simulación auth abusivo | OK: intentos controlados generaron `401`, luego lockout/`429` |
| Observabilidad | Se validaron eventos `SECURITY_AUTH_FAILURE`, `SECURITY_AUTH_LOCKOUT` y `SECURITY_RATE_LIMIT` |

### 2026-07-30 — Recorrido visible como operador

| Paso | Resultado | Observación |
|---|---|---|
| Registro proveedor 1 | OK | Email confirmado; usuario quedó pendiente de aprobación |
| Registro proveedor 2 | OK | Email confirmado; usuario quedó pendiente de aprobación |
| Aprobaciones admin | OK | Bandeja pasó de 2 solicitudes a 0 solicitudes |
| Creación subasta | OK | Se creó `2026/000002` desde `/licitaciones/subasta` usando nota de pedido existente |
| Invitación proveedores | OK | Se asignaron 2 proveedores: Olivera Servicios Generales SRL y Olivera Suministros SRL |
| Publicación/envío | OK | Subasta `2026/000002` pasó a estado `Enviada Pendiente` |
| Login proveedor 2 | OK | El usuario proveedor sólo visualiza CompraVenta Digital |
| Oferta proveedor 2 | OK | Oferta registrada en sala; resumen muestra representante y proveedor |
| Login admin posterior | OK | Se validó la variante correcta indicada por el usuario y se recuperó sesión de administración |
| Blanqueo proveedor 1 | OK | Se generó clave temporal desde `/admin/usuarios/activos` sin exponerla en el informe |
| Login proveedor 1 | OK | El representante ingresó correctamente luego del blanqueo |
| Oferta proveedor 1 | OK | Oferta registrada en `/compra-venta/subastas/15`; resumen mostró representante y proveedor |

### 2026-07-30 — Seguridad de autorización

| Prueba | Resultado | Observación |
|---|---|---|
| Proveedor navega a `/admin/seguridad` | OK parcial | El front redirige a `/modulos` y no muestra contenido administrativo |
| Proveedor navega a `/licitaciones` | OK parcial | El front redirige a `/modulos` y no muestra contenido de gestión |
| Proveedor navega a `/clasificadores` | OK parcial | El front redirige a `/modulos` y no muestra contenido de clasificadores |
| Proveedor llama APIs privadas sin token | OK | Endpoints protegidos devuelven `401`; endpoints públicos de vidriera devuelven `200` |
| Proveedor con token llama APIs admin/gestión | **Falla** | El backend respondió `200` en listados internos como usuarios activos, páginas/roles, cotizaciones y proveedores. El guard del front no alcanza: faltan checks de rol/página/scope en API |
| Rate limit reportes | OK | Con límite `6/300s`, las requests 7 y 8 devolvieron `429` con `Retry-After` y `correlationId` |
| Headers seguridad | OK con observación | `nosniff`, `Referrer-Policy`, `X-Frame-Options` y `frame-ancestors` presentes |
| CORS local | OK | Preflight desde `localhost:4201` respondió `204` con origin/method/headers esperados |
| Correlation ID response header | Observación | La respuesta devolvió `X-Correlation-ID` duplicado con el mismo valor; conviene normalizarlo a un único header |

### 2026-07-30 — Auditoría y persistencia correlacionada

Lectura sanitizada de auditoría posterior al recorrido:

| Tabla | Registros |
|---|---:|
| `auditoria.t_auditoria_datos` | 28 |
| `auditoria.t_logs_eventos` | 36 |

Movimientos correlacionados:

| Registro/evento | Tabla/acción | Movimiento correspondiente |
|---|---|---|
| `t_proveedores` `ADDED` | proveedores 17 y 18 | Alta de dos proveedores Olivera |
| `NUEVO_REGISTRO` | IAM | Registro de ambos representantes |
| `EMAIL_CONFIRMADO` | IAM | Confirmación de correo de ambos representantes |
| `USUARIO_APROBADO` | IAM | Aprobación de ambos representantes por admin |
| `t_cotizacion` `ADDED` + `SUBASTA_CREADA` | Licitaciones | Creación de subasta `2026/000002` |
| `t_cotizacion_especificacion` `ADDED` | Licitaciones | Especificaciones con `GestionDocumentacion=false` |
| `t_cotizacion_proveedor` `ADDED` | Licitaciones | Invitación/asignación de ambos proveedores |
| `SUBASTA_PUBLICADA` | Licitaciones | Publicación/envío de invitaciones |
| `t_ofertas_subasta` `ADDED` | Licitaciones | Oferta proveedor 2 y oferta proveedor 1 |
| `OFERTAS_PROCESADAS` | Licitaciones | Procesamiento exitoso de cada batch de oferta |
| `RESETEO_PASSWORD` | IAM | Blanqueo controlado para recuperar acceso de proveedor 1 |
| `SECURITY_RATE_LIMIT` | Gateway | Bloqueos 429 de login y reportes con `Retry-After` |
| `SECURITY_AUTH_FAILURE` / `SECURITY_AUTH_LOCKOUT` | IAM | Pruebas controladas de credenciales inválidas y lockout |

Riesgo detectado en auditoría:

- `t_auditoria_datos` / `t_logs_eventos` persisten identificadores personales y de negocio en JSON (emails, documentos, CUIT u otros campos de contacto). Para producción conviene aplicar minimización/masking por campo antes de persistir, no sólo al consultar.

### Hallazgos funcionales / UX / WCAG durante el recorrido

| Severidad | Hallazgo | Evidencia |
|---|---|---|
| Alta | Auditoría con PII sin minimización suficiente | Se observaron emails, documentos y CUIT en payloads JSON de auditoría/logs; en producción deberían persistirse enmascarados o minimizados |
| Alta | Autorización backend insuficiente para rol proveedor | Con JWT de proveedor, APIs internas/admin responden `200`; debe aplicarse autorización por rol, módulo/página y scope en backend/gateway |
| Media | `X-Correlation-ID` duplicado | El gateway devuelve el header repetido; no bloquea operación pero ensucia trazabilidad/soporte |
| Media | Botón de validar CUIT en registro proveedor es sólo ícono sin texto visible | El submit queda deshabilitado hasta validar CUIT, pero el botón no comunica claramente su acción |
| Baja | Mojibake visible en dashboard Compra/Venta | Columna `ACCIÃ³N` en tabla de calendario del módulo proveedor |
| Media | Identidad inconsistente en resumen de ofertas | Una oferta previa quedó como `Proveedor Anónimo / PROVEEDOR ANÓNIMO`; la nueva oferta del proveedor 1 sí mostró representante y proveedor |
| Positiva | Documentación opcional en sala | Cuando `gestionDocumentacion = false`, el cuadro sigue visible y explicita que no bloquea la oferta |
| Positiva | Control de autorización por módulo proveedor | Representante proveedor aprobado sólo vio CompraVenta Digital, no administración ni licitaciones |

### Estado del circuito de 2 proveedores

Circuito mínimo completado:

1. operador/admin creó y publicó subasta;
2. operador/admin invitó a dos proveedores;
3. proveedor 2 ingresó y ofertó;
4. proveedor 1 ingresó y ofertó;
5. la sala aceptó documentación opcional como no bloqueante;
6. queda pendiente correlacionar en auditoría cada movimiento con `t_auditoria_datos` / `t_logs_eventos` y ampliar pruebas BOLA/IDOR.
