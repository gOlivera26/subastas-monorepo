# PortalSubastas

Plataforma de subastas online con microservicios en .NET 8, arquitectura limpia y eventos distribuidos.

## Arquitectura

```
┌──────────────────────────────────────────────────────────────────────┐
│                     BACKEND (.NET 8 Microservices)                   │
│                                                                        │
│                    ┌──────────────────────┐                           │
│                    │      API Gateway      │                           │
│                    │      (YARP Proxy)     │                           │
│                    │        :5278          │                           │
│                    └────┬──────────┬───────                           │
│                         │          │                                    │
│              ┌──────────┘          └──────────┐                        │
│              ▼                                ▼                        │
│  ┌──────────────────────┐      ┌──────────────────────────────┐        │
│  │   Identity.API       │      │   Providers.API              │        │
│  │   :5252              │      │   :5280                      │        │
│  │                      │      │                              │        │
│  │  - Auth (JWT/2FA)    │      │  - Proveedores CRUD          │        │
│  │  - User Management   │      │  - Rubros (jerárquico)       │        │
│  │  - Roles/Orgs        │      │  - Domicilios                │        │
│  │  - IAM completo      │      │  - Constancia AFIP (R2)      │        │
│  └──────┬───────────────┘      └──────┬───────────────────────┘        │
│         │                             │                                │
│         │     MassTransit + RabbitMQ  │                                │
│         │                             │                                │
│         ▼                             ▼                                │
│  ┌──────────────────────────────────────────────────────────┐          │
│  │                   Audit.Worker                            │          │
│  │   (Background Service)                                    │          │
│  │                                                           │          │
│  │  - SystemLogEventConsumer                                 │          │
│  │  - DataChangedEventConsumer                               │          │
│  │                                                           │          │
│  │  Persiste en:                                             │          │
│  │  - auditoria.t_logs_eventos                               │          │
│  │  - auditoria.t_auditoria_datos                            │          │
│  └──────────────────────────────────────────────────────────┘          │
│                                                                        │
│  ┌──────────────────────────────────────────────────────────┐          │
│  │              PortalSubastas.Contracts                     │          │
│  │           Eventos compartidos (MassTransit)               │          │
│  │  SystemLogEvent | DataChangedEvent | UserApprovedEvent   │          │
│  ──────────────────────────────────────────────────────────┘          │
└────────────────────────────────────────────────────────────────────────┘
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

### Providers (Negocio) — ✅ Completo
- CRUD de Proveedores con verificación de CUIT
- Gestión de Rubros con jerarquía padre-hijo (3 niveles)
- Vista de árbol jerárquico de rubros
- Vinculación de rubros a proveedores
- CRUD de Domicilios (tipos, provincias)
- Carga de Constancia AFIP a Cloudflare R2
- Búsqueda y filtrado en todas las entidades
- Paginación y ordenamiento en todos los listados

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

## API Gateway — Rutas

```
/api/Auth/*         → Identity.API (:5252)
/api/User/*         → Identity.API (:5252)
/api/Role/*         → Identity.API (:5252)
/api/Organization/* → Identity.API (:5252)
/api/Provider/*     → Providers.API (:5280)
/api/Rubro/*        → Providers.API (:5280)
/api/Domicilio/*    → Providers.API (:5280)
```

## Eventos Distribuidos (MassTransit + RabbitMQ)

### Eventos

| Evento | Descripción | Consumidor |
|---|---|---|
| `SystemLogEvent` | Log de acciones del usuario (módulo, acción, IP) | `SystemLogEventConsumer` |
| `DataChangedEvent` | Auditoría de cambios en datos (INSERT/UPDATE/DELETE) | `DataChangedEventConsumer` |
| `UserApprovedEvent` | Evento específico de aprobación de usuario | — |

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

- **t_proveedores** — Proveedores (Razón Social, CUIT, CUP, email, tipo persona)
- **t_proveedores_representantes** — Vinculación usuario ↔ proveedor (Proveedor Directo)
- **t_rubros** — Rubros jerárquicos (código, descripción, padre, imputable)
- **t_proveedores_rubros** — Proveedores por rubro
- **t_domicilios** — Domicilios (calle, número, barrio, ciudad, provincia, CP)
- **t_tipos_domicilio** — Catálogo tipos de domicilio
- **t_provincias** — Catálogo de provincias

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

## Ejecutar en Desarrollo

### Con Docker Compose

```bash
# Levantar todos los servicios
docker-compose up --build

# Servicios disponibles:
# - Gateway:    http://localhost:5278
# - Identity:   http://localhost:5252
# - Providers:  http://localhost:5280
# - RabbitMQ:   http://localhost:15672 (guest/guest)
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

# 4. Audit Worker
cd PortalSubastas.Audit/PortalSubastas.Audit.Worker
dotnet run

# 5. API Gateway
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
