# PortalSubastas

Plataforma de subastas online con microservicios en .NET 8, arquitectura limpia y eventos distribuidos.

## Arquitectura

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                     BACKEND (.NET 8 Microservices)                           │
│                                                                              │
│                    ┌──────────────────────┐                                  │
│                    │      API Gateway      │                                  │
│                    │      (YARP Proxy)     │                                  │
│                    │        :5278          │                                  │
│                    └──┬────────┬────────┬──┘                                  │
│                       │        │        │                                     │
│            ┌──────────┘        │        └──────────┐                         │
│            ▼                   ▼                   ▼                         │
│ ┌────────────────┐  ┌────────────────┐  ┌────────────────────────┐           │
│ │ Identity.API   │  │ Providers.API  │  │ Licitaciones.API       │           │
│ │ :5252          │  │ :5280          │  │ :5282                  │           │
│ │                │  │                │  │                        │           │
│ │ - Auth(JWT/2FA)│  │ - Proveedores  │  │ - Subastas (CRUD)      │           │
│ │ - Users        │  │ - Rubros       │  │ - Ofertas (SignalR)    │           │
│ │ - Roles/Orgs   │  │ - Domicilios   │  │ - Ganadores            │           │
│ │ - IAM completo │  │ - AFIP (R2)    │  │ - Notas de Pedido      │           │
│ │                │  │                │  │ - Chat (SignalR)       │           │
│ │                │  │                │  │ - Documentos (R2)      │           │
│ └───────┬────────┘  └───────┬────────┘  └───────┬────────────────┘           │
│         │                   │                   │                            │
│         │     MassTransit + RabbitMQ            │                            │
│         │                   │                   │                            │
│         ▼                   ▼                   ▼                            │
│ ┌──────────────────────────────────────────────────────────┐                 │
│ │                   Audit.Worker                            │                 │
│ │   (Background Service)                                    │                 │
│ │                                                           │                 │
│ │  - SystemLogEventConsumer                                 │                 │
│ │  - DataChangedEventConsumer                               │                 │
│ │                                                           │                 │
│ │  Persiste en:                                             │                 │
│ │  - auditoria.t_logs_eventos                               │                 │
│ │  - auditoria.t_auditoria_datos                            │                 │
│ └──────────────────────────────────────────────────────────┘                 │
│                                                                              │
│ ┌──────────────────────────────────────────────────────────┐                 │
│ │              PortalSubastas.Contracts                     │                 │
│ │           Eventos compartidos (MassTransit)               │                 │
│ │  SystemLogEvent | DataChangedEvent | UserApprovedEvent   │                 │
│ ──────────────────────────────────────────────────────────┘                 │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Proyectos

| Proyecto | Capa | Descripción |
|---|---|---|
| `PortalSubastas.Identity.API` | Presentation | Web API IAM, controladores, middlewares |
| `PortalSubastas.Identity.Application` | Application | Servicios, DTOs, AutoMapper Profiles |
| `PortalSubastas.Identity.Domain` | Domain | Entidades, interfaces de auditoría, EF Core DbContext |
| `PortalSubastas.Providers.API` | Presentation | Web API de negocio, controladores, middlewares |
| `PortalSubastas.Providers.Application` | Application | Servicios, DTOs, AutoMapper Profiles |
| `PortalSubastas.Providers.Domain` | Domain | Entidades de negocio, EF Core DbContext |
| `PortalSubastas.Licitaciones.API` | Presentation | Web API de subastas, SignalR Hub, Worker, middlewares |
| `PortalSubastas.Licitaciones.Application` | Application | Servicios, DTOs, AutoMapper Profiles, Validators |
| `PortalSubastas.Licitaciones.Domain` | Domain | Entidades de subastas, EF Core DbContext |
| `PortalSubastas.Gateway` | Gateway | YARP reverse proxy, enrutamiento a microservicios |
| `PortalSubastas.Audit.Worker` | Worker | Worker service que consume eventos de auditoría vía RabbitMQ |
| `PortalSubastas.Contracts` | Shared | Contratos/eventos MassTransit compartidos entre servicios |

## Stack Tecnológico

- **.NET 8** — LTS
- **ASP.NET Core 8** — Web API + Background Worker
- **Entity Framework Core 8** — ORM con Npgsql (PostgreSQL)
- **AutoMapper 12** — Mapeo de entidades a DTOs
- **MassTransit 8.2 + RabbitMQ** — Eventos distribuidos
- **BCrypt** — Hashing de contraseñas
- **JWT Bearer + 2FA (TOTP)** — Autenticación stateless
- **YARP** — API Gateway reverse proxy
- **Cloudflare R2** — Almacenamiento de archivos (constancias AFIP)
- **Swagger/OpenAPI** — Documentación
- **OpenTelemetry** — Trazas, métricas y logs
- **xUnit + Moq + FluentAssertions** — Tests

## Módulos Completados

### Identity (IAM) — ✅ Completo
- Autenticación con JWT + 2FA (TOTP QR)
- Registro de usuarios con validación
- Gestión de usuarios activos y pendientes
- Aprobación/rechazo de usuarios
- Gestión de roles y módulos
- Vinculación de usuarios a entidades (Gestor/Proveedor)
- Blanqueo de contraseñas
- Auditoría de accesos por usuario
- Verificación de CUIT contra AFIP (mock)
- **AuditInterceptor**: captura automática de todos los cambios (INSERT/UPDATE/DELETE) y publicación de `DataChangedEvent`
- **PublishSystemLogAsync**: logs de negocio en 8 acciones críticas (registro, aprobación, reset password, cambio de rol, vinculación, etc.)

### Providers (Negocio) — ✅ Completo
- CRUD de Proveedores con verificación de CUIT
- Gestión de Rubros con jerarquía padre-hijo (3 niveles)
- Vista de árbol jerárquico de rubros
- Vinculación de rubros a proveedores
- CRUD de Domicilios (tipos, provincias)
- Carga de Constancia AFIP a Cloudflare R2
- Búsqueda y filtrado en todas las entidades
- Paginación y ordenamiento en todos los listados
- **AuditInterceptor**: captura automática de cambios en proveedores, rubros, domicilios y vínculos
- **PublishSystemLogAsync**: logs de negocio en 11 acciones (proveedor creado/actualizado, rubro creado/actualizado/eliminado, domicilio creado/actualizado/eliminado, constancia AFIP subida, rubro vinculado/desvinculado)

### Licitaciones (Subastas) — ✅ Completo
- CRUD de Cotizaciones (subastas) con auto-numérico y especificaciones
- Ciclo de vida completo: Generado → Publicada → En Curso → Finalizada/Desistida/Anulada
- Ofertas en tiempo real con SignalR (prórroga automática, cierre por tope de importe mínimo)
- Dashboard de subastas (en curso, próximas, del mes, búsqueda avanzada)
- Gestión de ganadores por ítem o renglón
- Carga de garantías/pagarés a Cloudflare R2
- Sistema de consultas y respuestas en tiempo real (chat SignalR)
- Documentación de ítems/renglones con envío definitivo
- Pliegos y documentos de subasta (Cloudflare R2)
- Notas de pedido (Reservas) con autorización y clonación
- Catálogos: bienes, objetos de gasto, categorías programáticas, monedas, estados
- **AuditInterceptor**: captura automática de cambios en todas las entidades de subastas
- **PublishSystemLogAsync**: logs de negocio en 28 acciones críticas (subasta creada/publicada/finalizada/desistida/prorrogada, ofertas procesadas, ganador registrado, garantía subida, pregunta realizada/respondida, documentación enviada, pliego subido, nota de pedido creada/autorizada/clonada, etc.)
- **SubastaCloserWorker**: background service que cierra subastas automáticamente al vencer el plazo
- **SignalR Hub**: notificaciones en tiempo real de nuevas ofertas, cierres y prórrogas

## Identity API — Endpoints

```
# Autenticación
POST   /api/auth/login              → Inicio de sesión (JWT + 2FA)
POST   /api/auth/register           → Registro de usuario
POST   /api/auth/verify-2fa         → Verificar código 2FA
GET    /api/auth/profile            → Perfil del usuario autenticado
POST   /api/auth/change-password    → Cambio de contraseña
PUT    /api/auth/profile            → Actualizar perfil

# Usuarios
GET    /api/user/pending            → Usuarios pendientes de aprobación
GET    /api/user/active             → Usuarios activos (paginado + sorting)
POST   /api/user/{id}/approve       → Aprobar usuario
POST   /api/user/{id}/reset-password → Resetear contraseña
PUT    /api/user/{id}/role          → Cambiar rol
POST   /api/user/{id}/link          → Vincular entidad (Gestor/Proveedor)
POST   /api/user/{id}/unlink        → Desvincular entidad
GET    /api/user/{id}/audit         → Auditoría del usuario

# Catálogos
GET    /api/role/active             → Roles activos
GET    /api/organization/active     → Organizaciones activas
GET    /api/provider/verify/{cuit}  → Verificar CUIT de proveedor
```

## Providers API — Endpoints

```
# Proveedores
GET    /api/Provider                → Listado paginado + sorting
POST   /api/Provider                → Crear proveedor
PUT    /api/Provider                → Actualizar proveedor
GET    /api/Provider/verify/{cuit}  → Verificar CUIT
GET    /api/Provider/{id}/rubros    → Rubros vinculados
POST   /api/Provider/{id}/rubros    → Vincular rubros
DELETE /api/Provider/{id}/rubros/{rubroId} → Desvincular rubro
POST   /api/Provider/{id}/constancia-afip → Subir constancia AFIP (R2)
GET    /api/Provider/{id}/afip/verify/{cuit} → Verificar CUIT en AFIP

# Rubros
GET    /api/Rubro                   → Listado paginado + sorting
POST   /api/Rubro                   → Crear rubro
PUT    /api/Rubro                   → Actualizar rubro
DELETE /api/Rubro/{id}              → Eliminar rubro
GET    /api/Rubro/tree              → Árbol jerárquico completo
GET    /api/Rubro/{parentId}/children → Hijos de un rubro
GET    /api/Rubro/search?q=         → Búsqueda de rubros

# Domicilios
GET    /api/Domicilio/persona/{personaId} → Domicilios de una persona
POST   /api/Domicilio/persona/{personaId} → Crear domicilio
PUT    /api/Domicilio               → Actualizar domicilio
DELETE /api/Domicilio/{id}          → Eliminar domicilio
GET    /api/Domicilio/tipos-domicilio → Tipos de domicilio
GET    /api/Domicilio/provincias    → Provincias
```

## Licitaciones API — Endpoints

```
# Subastas (Cotizaciones)
GET    /api/Cotizacion                       → Listado con filtros
POST   /api/Cotizacion                       → Crear subasta
PUT    /api/Cotizacion/{id}                  → Actualizar subasta
DELETE /api/Cotizacion/{id}                  → Anular subasta (solo estado Generado)
GET    /api/Cotizacion/{id}                  → Detalle completo
POST   /api/Cotizacion/{id}/notificar        → Publicar subasta
POST   /api/Cotizacion/{id}/finalizar        → Finalizar subasta
POST   /api/Cotizacion/{id}/desistir         → Desistir subasta
POST   /api/Cotizacion/{id}/prorrogar        → Prorrogar (minutos)
POST   /api/Cotizacion/{id}/desistir-participacion → Proveedor se desiste
GET    /api/Cotizacion/{id}/metricas-ahorro  → Métricas de ahorro

# Dashboard
GET    /api/Cotizacion/dashboard/en-curso    → Subastas en curso
GET    /api/Cotizacion/dashboard/proximas    → Próximas subastas
GET    /api/Cotizacion/dashboard/del-mes     → Subastas del mes
GET    /api/Cotizacion/buscar                → Búsqueda avanzada

# Ofertas
GET    /api/OfertaSubasta/{idCotizacion}     → Historial de ofertas
POST   /api/OfertaSubasta/{idCotizacion}/Batch → Procesar ofertas (batch)
GET    /api/OfertaSubasta/MisOfertas         → Mis ofertas (proveedor)

# Ganadores
GET    /api/Ganador/{idCotizacion}           → Ganadores de una subasta
POST   /api/Ganador                          → Registrar ganador
DELETE /api/Ganador/{id}                     → Eliminar ganador

# Garantías
GET    /api/Garantia/{idCotizacion}          → Garantías de una subasta
POST   /api/Garantia                         → Subir garantía/pagaré (R2)
DELETE /api/Garantia/{id}                    → Eliminar garantía

# Consultas (Chat)
GET    /api/Consulta/{idCotizacion}          → Consultas de una subasta
POST   /api/Consulta/{idCotizacion}/pregunta → Realizar pregunta
POST   /api/Consulta/{idCotizacion}/respuesta/{idMensaje} → Responder pregunta

# Documentación de ítems
GET    /api/DocumentoItem                    → Documentos de un ítem/renglón
POST   /api/DocumentoItem                    → Subir documento (R2)
DELETE /api/DocumentoItem/{id}               → Eliminar documento
POST   /api/DocumentoItem/enviar-definitiva  → Enviar documentación definitiva

# Pliegos/Documentos de subasta
GET    /api/CotizacionDocumento/{idCotizacion} → Documentos de una subasta
POST   /api/CotizacionDocumento              → Subir pliego (R2)
DELETE /api/CotizacionDocumento/{id}         → Eliminar pliego

# Notas de Pedido (Reservas)
GET    /api/Reserva                          → Listado con filtros
POST   /api/Reserva                          → Crear nota de pedido
PUT    /api/Reserva/{id}                     → Actualizar nota de pedido
DELETE /api/Reserva/{id}                     → Anular nota de pedido
POST   /api/Reserva/{id}/autorizar           → Autorizar nota de pedido
POST   /api/Reserva/{id}/clonar              → Clonar nota de pedido

# Ítems de Nota de Pedido
GET    /api/ReservaDetalle/{idReserva}       → Ítems de una nota
POST   /api/ReservaDetalle                   → Agregar ítem
PUT    /api/ReservaDetalle/{id}              → Modificar ítem
DELETE /api/ReservaDetalle/{id}              → Eliminar ítem
POST   /api/ReservaDetalle/{id}/desautorizar → Desautorizar ítem

# Catálogos (read-only)
GET    /api/CatalogoBien                     → Catálogo de bienes
GET    /api/ObjetoGasto                      → Objetos de gasto
GET    /api/CategoriaProgramatica            → Categorías programáticas
GET    /api/Moneda                           → Monedas
GET    /api/Estado                           → Estados

# SignalR
WS     /signalr/subastas                     → Hub de notificaciones en tiempo real
```

## API Gateway — Rutas

```
/api/Auth/*              → Identity.API (:5252)
/api/User/*              → Identity.API (:5252)
/api/Role/*              → Identity.API (:5252)
/api/Organization/*      → Identity.API (:5252)
/api/Provider/*          → Providers.API (:5280)
/api/Rubro/*             → Providers.API (:5280)
/api/Domicilio/*         → Providers.API (:5280)
/api/Cotizacion/*        → Licitaciones.API (:5282)
/api/OfertaSubasta/*     → Licitaciones.API (:5282)
/api/Ganador/*           → Licitaciones.API (:5282)
/api/Garantia/*          → Licitaciones.API (:5282)
/api/Consulta/*          → Licitaciones.API (:5282)
/api/DocumentoItem/*     → Licitaciones.API (:5282)
/api/CotizacionDocumento/* → Licitaciones.API (:5282)
/api/Reserva/*           → Licitaciones.API (:5282)
/api/ReservaDetalle/*    → Licitaciones.API (:5282)
/api/CatalogoBien/*      → Licitaciones.API (:5282)
/api/ObjetoGasto/*       → Licitaciones.API (:5282)
/api/CategoriaProgramatica/* → Licitaciones.API (:5282)
/api/Moneda/*            → Licitaciones.API (:5282)
/api/Estado/*            → Licitaciones.API (:5282)
/signalr/subastas        → Licitaciones.API (:5282) SignalR Hub
```

## Eventos Distribuidos (MassTransit + RabbitMQ)

### Eventos

| Evento | Descripción | Consumidor |
|---|---|---|
| `SystemLogEvent` | Log de acciones del usuario (módulo, acción, IP) | `SystemLogEventConsumer` |
| `DataChangedEvent` | Auditoría de cambios en datos (INSERT/UPDATE/DELETE) | `DataChangedEventConsumer` |
| `UserApprovedEvent` | Evento específico de aprobación de usuario | — |

### Acciones de Auditoría por Módulo

**Identity (8 acciones):** `INICIO_SESION`, `NUEVO_REGISTRO`, `CAMBIO_PASSWORD`, `USUARIO_APROBADO`, `RESETEO_PASSWORD`, `USUARIO_DESVINCULADO`, `CAMBIO_ROL`, `USUARIO_VINCULADO`

**Providers (11 acciones):** `PROVEEDOR_CREADO`, `PROVEEDOR_ACTUALIZADO`, `RUBRO_VINCULADO`, `RUBRO_DESVINCULADO`, `CONSTANCIA_AFIP_SUBIDA`, `RUBRO_CREADO`, `RUBRO_ACTUALIZADO`, `RUBRO_ELIMINADO`, `DOMICILIO_CREADO`, `DOMICILIO_ACTUALIZADO`, `DOMICILIO_ELIMINADO`

**Licitaciones (28 acciones):** `SUBASTA_CREADA`, `SUBASTA_MODIFICADA`, `SUBASTA_ANULADA`, `SUBASTA_PUBLICADA`, `SUBASTA_FINALIZADA`, `SUBASTA_DESISTIDA`, `SUBASTA_PRORROGADA`, `PROVEEDOR_DESISTE_SUBASTA`, `OFERTAS_PROCESADAS`, `GANADOR_REGISTRADO`, `GANADOR_ELIMINADO`, `GARANTIA_SUBIDA`, `GARANTIA_ELIMINADA`, `PREGUNTA_REALIZADA`, `PREGUNTA_RESPONDIDA`, `DOCUMENTO_ITEM_SUBIDO`, `DOCUMENTO_ITEM_ELIMINADO`, `DOCUMENTACION_DEFINITIVA_ENVIADA`, `PLIEGO_SUBIDO`, `PLIEGO_ELIMINADO`, `CREAR_NOTA_PEDIDO`, `MODIFICAR_NOTA_PEDIDO`, `ELIMINAR_NOTA_PEDIDO`, `AUTORIZACION_NOTA_PEDIDO`, `CLONAR_NOTA_PEDIDO`, `AGREGAR_ITEM_NOTA`, `MODIFICAR_ITEM_NOTA`, `ELIMINAR_ITEM_NOTA`, `DESAUTORIZAR_ITEM`

### Colas RabbitMQ

| Cola | Consumidor |
|---|---|
| `audit-system-logs-queue` | `SystemLogEventConsumer` |
| `audit-data-changes-queue` | `DataChangedEventConsumer` |

### Consumers del Audit Worker

**SystemLogEventConsumer** — Persiste en `auditoria.t_logs_eventos`:
- `id_usuario`, `nombre_usuario`, `accion`, `modulo`, `detalles` (JSONB), `ip_origen`, `fecha_hora`

**DataChangedEventConsumer** — Persiste en `auditoria.t_auditoria_datos`:
- `fecha_hora`, `id_usuario`, `tabla_afectada`, `registro_id`, `tipo_operacion`, `valores_anteriores` (JSONB), `valores_nuevos` (JSONB)

## Base de Datos — Esquemas

### Esquema IAM (`iam` schema)

- **t_usuarios** — Usuarios del sistema (GUID PK, email único, password hash, estado, aprobación, 2FA)
- **t_personas** — Datos personales (nombre, apellido, documento, teléfono)
- **t_roles** — Roles del sistema
- **t_modulos** — Módulos de la aplicación
- **t_roles_modulos** — Permisos (roles → módulos)
- **t_organizaciones** — Organizaciones/entidades
- **t_jurisdicciones_usuarios** — Vinculación usuario ↔ organización (Gestor Licitación)
- **t_estados_usuario** — Catálogo de estados (Activo, Pendiente, Inactivo, Bloqueado, Baja)
- **t_tipos_documento** — Catálogo tipos de documento
- **t_tipos_persona** — Catálogo tipos de persona

### Esquema Negocio (`negocio` schema)

**Tablas de Providers:**
- **t_proveedores** — Proveedores (Razón Social, CUIT, CUP, email, tipo persona)
- **t_proveedores_representantes** — Vinculación usuario ↔ proveedor (Proveedor Directo)
- **t_rubros** — Rubros jerárquicos (código, descripción, padre, imputable)
- **t_proveedores_rubros** — Proveedores por rubro
- **t_domicilios** — Domicilios (calle, número, barrio, ciudad, provincia, CP)
- **t_tipos_domicilio** — Catálogo tipos de domicilio
- **t_provincias** — Catálogo de provincias

**Tablas de Licitaciones:**
- **t_cotizacion** — Subastas (nro_cotizacion, estado, tipo contratación, vigencia, unidad adm.)
- **t_cotizacion_especificacion** — Especificaciones de subasta (fechas, margen mejora, prórroga, redeterminación)
- **t_cotizacion_detalle** — Ítems de subasta (vinculados a reserva_detalle o renglón)
- **t_cotizacion_renglon** — Renglones/lotes de subasta
- **t_cotizacion_proveedor** — Proveedores invitados/inscritos a una subasta
- **t_ofertas_subasta** — Ofertas de proveedores (monto, fecha, ítem/renglón)
- **t_ganador** — Ganadores adjudicados por ítem o renglón
- **t_mensajes** — Consultas y respuestas de proveedores (chat)
- **t_garantias_subasta** — Garantías/pagarés subidos a R2
- **t_cotizacion_documento** — Pliegos y documentos de subasta (R2)
- **t_documento_item_proveedor** — Documentación de ítems/renglones por proveedor (R2)
- **t_reservas** — Notas de pedido (nro_reserva, estado, unidad adm., sub-responsable)
- **t_reserva_detalle** — Ítems de nota de pedido (cantidad, importe, moneda, objeto de gasto)
- **t_catalogos_bien** — Catálogo de bienes (n_item, código, objeto de gasto)
- **t_objetos_gasto** — Objetos de gasto (jerárquico, imputa ejecución)
- **t_categorias_programaticas** — Categorías programáticas (jerárquico)
- **t_sub_responsables** — Sub-responsables de notas de pedido
- **t_unidades_administrativas** — Unidades administrativas
- **t_vigencias** — Vigencias/ejercicios fiscales
- **t_moneda** — Catálogo de monedas
- **t_estados** — Catálogo de estados (reservas, subastas, ítems)

### Esquema Auditoría (`auditoria` schema)

- **t_logs_eventos** — Logs de eventos del sistema
- **t_auditoria_datos** — Auditoría de cambios en datos

## Estructura del Proyecto

```
PortalSubastas/
├── PortalSubastas.Identity/           # Microservicio IAM
│   ├── PortalSubastas.Identity.API/
│   ├── PortalSubastas.Identity.Application/
│   ├── PortalSubastas.Identity.Domain/
│   └── tests/
├── PortalSubastas.Providers/          # Microservicio de Negocio
│   ├── PortalSubastas.Providers.API/
│   ├── PortalSubastas.Providers.Application/
│   ├── PortalSubastas.Providers.Domain/
│   └── tests/
├── PortalSubastas.Licitaciones/       # Microservicio de Subastas
│   ├── PortalSubastas.Licitaciones.API/
│   ├── PortalSubastas.Licitaciones.Application/
│   ├── PortalSubastas.Licitaciones.Domain/
│   └── tests/
├── PortalSubastas.Gateway/            # API Gateway (YARP)
├── PortalSubastas.Audit/              # Worker de Auditoría
│   └── PortalSubastas.Audit.Worker/
├── PortalSubastas.Contracts/          # Eventos compartidos
└── docker-compose.yml                 # Orquestación Docker
```

## Configuración

### appsettings.json (Identity.API)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=portal_subastas;Username=usr;Password=pass"
  },
  "Jwt": {
    "Issuer": "PortalSubastas.Identity",
    "Audience": "PortalSubastas.Identity",
    "SecretKey": "clave-de-32-caracteres-minimo",
    "Minutes": "60"
  }
}
```

### appsettings.json (Providers.API)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=portal_subastas;Username=usr;Password=pass"
  },
  "R2": {
    "AccountId": "...",
    "AccessKeyId": "...",
    "SecretAccessKey": "...",
    "BucketName": "constancias-afip"
  }
}
```

### appsettings.json (Gateway)

```json
{
  "ReverseProxy": {
    "Routes": {
      "identity": { "ClusterId": "identity", "Match": { "Path": "/api/{**catch-all}" } },
      "providers": { "ClusterId": "providers", "Match": { "Path": "/api/Provider/{**catch-all}" } }
    },
    "Clusters": {
      "identity": { "Destinations": { "identity1": { "Address": "http://identity:5252" } } },
      "providers": { "Destinations": { "providers1": { "Address": "http://providers:5280" } } }
    }
  }
}
```

### appsettings.json (Licitaciones.API)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=portal_subastas;Username=usr;Password=pass"
  },
  "CloudflareR2": {
    "AccessKey": "...",
    "SecretKey": "...",
    "AccountId": "...",
    "PublicDomain": "https://pub-xxx.r2.dev",
    "BucketName": "subasta-electronica"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

## Ejecutar en Desarrollo

### Con Docker Compose

```bash
# Levantar todos los servicios
docker-compose up --build

# Servicios disponibles:
# - Gateway:       http://localhost:5278
# - Identity:      http://localhost:5252
# - Providers:     http://localhost:5280
# - Licitaciones:  http://localhost:5282
# - RabbitMQ:      http://localhost:15672 (guest/guest)
```

### Desarrollo individual

```bash
# 1. RabbitMQ (Docker)
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management

# 2. Identity API
cd PortalSubastas.Identity/PortalSubastas.Identity.API
dotnet run

# 3. Providers API
cd PortalSubastas.Providers/PortalSubastas.Providers.API
dotnet run

# 4. Licitaciones API
cd PortalSubastas.Licitaciones/PortalSubastas.Licitaciones.API
dotnet run

# 5. Audit Worker
cd PortalSubastas.Audit/PortalSubastas.Audit.Worker
dotnet run

# 6. API Gateway
cd PortalSubastas.Gateway/PortalSubastas.Gateway
dotnet run
```

## Convenciones de Desarrollo

- **Arquitectura limpia** — Separación en API/Application/Domain
- **Sin try-catch en servicios** — Middleware global maneja excepciones
- **BaseService** — Lógica genérica de paginación, auditoría y cache
- **AutoMapper** — Mapeo automático de entidades a DTOs
- **MassTransit** — Eventos para auditoría, no para lógica de negocio
- **DateTimeKind.Unspecified** — Para evitar errores de timezone con PostgreSQL
- **GlobalUsings** — Namespaces comunes centralizados para limpiar las clases

## Licencia

Privado — PortalSubastas
