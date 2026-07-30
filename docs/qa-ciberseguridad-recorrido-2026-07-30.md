# QA funcional y ciberseguridad — recorrido MCP

Fecha: 2026-07-30  
Proyecto: OWEN / PortalSubastas  
Modo: recorrido funcional + ciberseguridad con navegador visible, BD de prueba.

## Alcance

- Validar que el front y back estén navegables.
- Borrar auditoría/logs para iniciar trazabilidad desde cero.
- Recorrer pantallas principales con MCP del navegador visible.
- Ejecutar movimientos no destructivos / ABM controlados.
- Leer auditoría y correlacionar cada movimiento contra `auditoria.t_auditoria_datos` y `auditoria.t_logs_eventos`.
- Registrar hallazgos funcionales, seguridad, WCAG/UX y errores técnicos.

## Criterio de datos para el recorrido visible

A partir de esta pasada, el recorrido se realiza como operador real del sistema:

- No se usan nombres con prefijos `QA`, `TEST`, `AUTO` ni identificadores artificiales visibles.
- Se prioriza navegar, consultar, filtrar y abrir detalles con datos existentes.
- Si hace falta ejecutar un ABM, se usan nombres verosímiles de negocio y se documenta el movimiento.
- Las acciones se correlacionan en auditoría por usuario, fecha/hora, tabla, operación y pantalla recorrida.
- No se ejecutan borrados masivos ni bajas físicas durante el recorrido funcional.

---

## Estado inicial

### Auditoría reiniciada

Se ejecutó limpieza de auditoría/logs con `RESTART IDENTITY`.

| Tabla | Antes | Después |
|---|---:|---:|
| `auditoria.t_auditoria_datos` | 0 | 0 |
| `auditoria.t_logs_eventos` | 0 | 0 |

Secuencias:

| Secuencia | Estado |
|---|---|
| `auditoria.t_auditoria_datos_id_seq` | `last_value=1`, `is_called=false` |
| `auditoria.t_logs_eventos_id_seq` | `last_value=1`, `is_called=false` |

---

## Recorrido funcional visible

| Módulo / Pantalla | Ruta | Resultado |
|---|---|---|
| Centro de Comando | `/modulos` | OK |
| Inicio Licitaciones | `/licitaciones` | OK |
| Listado Subastas | `/licitaciones/subasta` | OK |
| Informes | `/licitaciones/informes` | OK |
| Tablero | `/licitaciones/tablero` | OK |
| Inicio Proveedores | `/proveedores` | OK |
| Listado Proveedores | `/proveedores/listado` | Error visible: `Error al cargar` |
| Rubros | `/proveedores/rubros-list` | OK |
| Inicio Compra/Venta | `/compra-venta` | OK |
| Subastas Compra/Venta | `/compra-venta/subastas` | OK |
| Inicio Clasificadores | `/clasificadores` | OK |
| Catálogo Bienes | `/clasificadores/catalogo-bienes` | OK |
| Categorías Programáticas | `/clasificadores/categorias-programaticas` | OK |
| Inicio Admin | `/admin` | OK |
| Seguridad | `/admin/seguridad` | OK |

### Licitaciones / Subasta

Se abrió el listado `/licitaciones/subasta` y se validó una subasta existente:

- Número: `2026/000001`
- Objeto: `Prueba subasta`
- Estado: `Generado`
- Modalidad: `Pública`
- Criterio: `Por Ítem`

Acciones revisadas sin modificar datos:

| Acción | Resultado |
|---|---|
| Ver Detalle | Abrió correctamente |
| Especificaciones | Abrió correctamente |
| Documentación en especificaciones | Visible como `OPCIONAL` |
| Mojibake visible | No detectado en detalle/especificaciones |

### Compra/Venta

Se abrió `/compra-venta/subastas` y luego la sala desde acción `Subastar`.

Ruta resultante: `/compra-venta/subastas/14`

Resultado:

- La sala abrió sin error técnico visible.
- La sala figura como `FINALIZADA` / `SALA CERRADA`.
- Se muestra panel de ofertas por ítem.

Hallazgo funcional:

- El selector de moneda en la sala muestra monedas inactivas (`EUR` recién dada de baja e incluso una moneda vieja de prueba).  
  Esto indica que la sala no estaría filtrando correctamente por monedas activas.

---

## ABM controlado con dato realista

Pantalla: `/clasificadores/monedas`  
Entidad: `t_moneda`  
Dato usado: `EUR / Euro`

| Paso | Acción | Resultado visible |
|---|---|---|
| 1 | Alta de moneda `EUR / Euro` | `Guardado.` |
| 2 | Edición de descripción | Guardado sin errores |
| 3 | Baja lógica de moneda | `Eliminado.` y registro queda `Inactivo` |

No se realizó baja física.

---

## Auditoría correlacionada

Después del ABM:

| Tabla | Registros |
|---|---:|
| `auditoria.t_auditoria_datos` | 3 |
| `auditoria.t_logs_eventos` | 0 |

### `auditoria.t_auditoria_datos`

| ID | Tabla | Operación | Registro | Movimiento correlacionado |
|---:|---|---|---|---|
| 1 | `t_moneda` | `ADDED` | `3` | Alta de `EUR / Euro` |
| 2 | `t_moneda` | `MODIFIED` | `3` | Edición de descripción |
| 3 | `t_moneda` | `MODIFIED` | `3` | Baja lógica (`FecBaja` pasa de null a fecha) |

Detalle observado:

- La baja se audita como `MODIFIED`, no como `DELETED`, porque es baja lógica.
- Usuario registrado: `00000000-0000-0000-0000-000000000001`.

---

## Pruebas de ciberseguridad no destructivas

Se ejecutó una mini pasada no destructiva contra gateway/API.

### Login incorrecto / lockout

| Prueba | Resultado |
|---|---|
| Login incorrecto sobre usuario existente | `401` |
| Intentos con usuario inexistente plausible | `401` en primeros intentos |
| Desde 5to intento | `429` |

### Rate limit reportes

| Prueba | Resultado |
|---|---|
| `GET /api/Reporte/catalogo` sin token | `401` inicialmente |
| Luego de varias solicitudes | `429` con `Retry-After=60` |

### Auditoría de seguridad generada

Después de la mini pasada:

| Tabla | Registros |
|---|---:|
| `auditoria.t_auditoria_datos` | 3 |
| `auditoria.t_logs_eventos` | 9 |

#### `auditoria.t_logs_eventos`

| ID | Acción | Módulo | Correlación |
|---:|---|---|---|
| 1 | `SECURITY_AUTH_FAILURE` | IAM | Login incorrecto sobre usuario existente |
| 2-5 | `SECURITY_AUTH_FAILURE` | IAM | Intentos fallidos sobre usuario inexistente plausible |
| 6 | `SECURITY_AUTH_LOCKOUT` | IAM | Lockout disparado |
| 7-9 | `SECURITY_AUTH_LOCKOUT` | IAM | Lockout activo |

Observación:

- Los emails se guardan enmascarados (`ad***`, `op***`), lo cual está bien.
- El sistema registra `Path`, `Method`, `UserAgent`, `CorrelationId` e IP origen.

Hallazgo pendiente:

- Los rate limits de reportes generados por gateway no aparecieron persistidos en `t_logs_eventos` como `SECURITY_RATE_LIMIT`.  
  El control HTTP funciona, pero falta trazabilidad persistente de esos rechazos.

---

## Error técnico confirmado: Proveedores

Pantalla afectada: `/proveedores/listado`

Resultado visible:

- `Error al cargar`

Logs del contenedor `providers-api`:

```text
Error no controlado: Value cannot be null. (Parameter 'AccessKey R2 faltante')
PortalSubastas.Providers.Application.Services.Implementations.FileStorageService..ctor(...):line 15
```

Interpretación:

- El listado de proveedores falla por configuración faltante de R2.
- El servicio intenta resolver `FileStorageService` y rompe antes de devolver datos.
- Para desarrollo/local debería degradar o mockear storage, no romper el listado.

---

## Accesibilidad / WCAG rápida

Pasada semántica sobre rutas críticas.

| Ruta | Hallazgo |
|---|---|
| `/modulos` | H1 OK, pero hay salto a H3 en cards. |
| `/licitaciones/subasta` | 2 inputs visibles sin nombre accesible claro. |
| `/compra-venta/subastas` | 2 inputs visibles sin nombre accesible claro. |
| `/compra-venta/subastas/14` | H1 vacío; 2 inputs sin nombre accesible claro. |
| `/clasificadores/catalogo-bienes` | Muchos botones sin nombre accesible, probablemente expand/collapse/acciones icon-only. |
| `/clasificadores/categorias-programaticas` | Muchos botones sin nombre accesible, probablemente expand/collapse/acciones icon-only. |
| `/licitaciones/informes` | Estructura de headings OK. |
| `/admin/seguridad` | H1 OK. |

Mojibake visible en esta pasada:

- No se detectó mojibake visible en las rutas recorridas.

Mojibake de fuente pendiente de pasada anterior:

- `src/app/features/compra-venta/dashboard/dashboard.component.ts`
- `src/app/features/compra-venta/dashboard/dashboard.component.html`

---

## Hallazgos priorizados

### Crítico

| Hallazgo | Detalle |
|---|---|
| Auditoría de usuarios guarda campos sensibles | En pasada anterior se detectó que `t_auditoria_datos` puede persistir `PasswordHash` en JSON de `t_usuarios`. Hay que excluir campos sensibles antes de persistir auditoría. |

### Alto

| Hallazgo | Detalle |
|---|---|
| Providers rompe por R2 faltante | `/proveedores/listado` falla con 500. |
| Monedas inactivas visibles en sala | Compra/Venta muestra monedas inactivas en selector de ofertas. |
| Falta persistir rate limits gateway | Los 429 de gateway no aparecen como eventos auditables. |

### Medio

| Hallazgo | Detalle |
|---|---|
| Healthchecks incompletos | Gateway sin `/health`; Licitaciones health protegido en pasada anterior. |
| Inputs sin nombre accesible | Afecta WCAG y navegación por lector de pantalla. |
| Botones icon-only sin nombre | Afecta árboles de catálogo/categorías. |

### Bajo / limpieza

| Hallazgo | Detalle |
|---|---|
| Dato viejo de prueba en monedas | Existe moneda inactiva con nombre artificial de una pasada anterior. Conviene limpiarla con criterio si molesta en pantallas. |
| H1 vacío en sala Compra/Venta | Mejora semántica/SEO/accesibilidad. |

---

## Recomendaciones próximas

1. Corregir auditoría para excluir `PasswordHash`, tokens, códigos y secretos antes de persistir JSON.
2. Corregir `FileStorageService` / R2 para que Providers no rompa en local/dev.
3. Filtrar monedas inactivas en sala de Compra/Venta y cualquier select de operación.
4. Persistir eventos `SECURITY_RATE_LIMIT` cuando gateway bloquea.
5. Completar nombres accesibles (`aria-label`/title/texto) en botones icon-only.
6. Corregir H1 vacío en `/compra-venta/subastas/:id`.
7. Corregir mojibake de fuente pendiente en dashboard Compra/Venta.
8. Limpiar datos artificiales previos en catálogos si se confirma que no son necesarios.
