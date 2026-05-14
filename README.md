# PortalSubastas

Plataforma de subastas online con microservicios en .NET 8, arquitectura limpia y eventos distribuidos.

## Arquitectura

```
┌──────────────────────┐     ┌──────────────────────────────────┐
│   Identity.API       │     │   Audit.Worker                   │
│   (ASP.NET Core 8)   │────▶│   (Background Service)           │
│                      │     │                                  │
│  - Auth (JWT)        │     │  - SystemLogEventConsumer        │
│  - User CRUD         │     │  - DataChangedEventConsumer      │
│  - Roles/Orgs        │     │                                  │
│  - Gestión IAM       │     │  Persiste en:                    │
└──────┬───────────────┘     │  - auditoria.t_logs_eventos      │
       │                     │  - auditoria.t_auditoria_datos   │
       │ MassTransit         └──────────────────────────────────┘
       │ RabbitMQ
       ▼
┌──────────────────────────────────────────────────────────┐
│                   PortalSubastas.Contracts                │
│              Eventos compartidos (MassTransit)            │
│  SystemLogEvent | DataChangedEvent | UserApprovedEvent   │
└──────────────────────────────────────────────────────────┘
```

## Proyectos

| Proyecto | Capa | Descripción |
|---|---|---|
| `PortalSubastas.Identity.API` | Presentation | Web API, controladores, middlewares |
| `PortalSubastas.Identity.Application` | Application | Servicios, DTOs, AutoMapper Profiles |
| `PortalSubastas.Identity.Domain` | Domain | Entidades, interfaces de auditoría, EF Core DbContext |
| `PortalSubastas.Audit.Worker` | Worker | Worker service que consume eventos de auditoría vía RabbitMQ |
| `PortalSubastas.Contracts` | Shared | Contratos/eventos MassTransit compartidos entre servicios |

## Stack Tecnológico

- **.NET 8** — LTS
- **ASP.NET Core 8** — Web API + Background Worker
- **Entity Framework Core 8** — ORM con Npgsql (PostgreSQL)
- **AutoMapper 12** — Mapeo de entidades a DTOs
- **MassTransit 8.2 + RabbitMQ** — Eventos distribuidos
- **BCrypt** — Hashing de contraseñas
- **JWT Bearer** — Autenticación stateless
- **Swagger/OpenAPI** — Documentación
- **OpenTelemetry** — Trazas, métricas y logs
- **xUnit + Moq + FluentAssertions** — Tests

## Identity API — Endpoints principales

```
POST   /api/auth/login              → Inicio de sesión
POST   /api/auth/register           → Registro de usuario
GET    /api/auth/profile            → Perfil del usuario autenticado
POST   /api/auth/change-password    → Cambio de contraseña
PUT    /api/auth/profile            → Actualizar perfil

GET    /api/user/pending            → Usuarios pendientes de aprobación
GET    /api/user/active             → Usuarios activos (paginado)
POST   /api/user/{id}/approve       → Aprobar usuario
POST   /api/user/{id}/reset-password → Resetear contraseña
PUT    /api/user/{id}/role          → Cambiar rol
POST   /api/user/{id}/link          → Vincular entidad (Gestor/Proveedor)
DELETE /api/user/{id}/unlink        → Desvincular entidad
GET    /api/user/{id}/audit         → Auditoría del usuario

GET    /api/role/active             → Roles activos
GET    /api/organization/active     → Organizaciones activas
GET    /api/provider/verify/{cuit}  → Verificar CUIT de proveedor
```

## Eventos Distribuidos (MassTransit + RabbitMQ)

### Arquitectura de Eventos

```
Identity.API                          Audit.Worker
     │                                     │
     │  publish(SystemLogEvent)            │
     │────────────────────────────────────▶│  → INSERT auditoria.t_logs_eventos
     │                                     │
     │  publish(DataChangedEvent)          │
     │────────────────────────────────────▶│  → INSERT auditoria.t_auditoria_datos
     │                                     │
     │  publish(UserApprovedEvent)         │
     │────────────────────────────────────▶│  (consumido si está configurado)
```

### Eventos

| Evento | Descripción | Consumidor |
|---|---|---|
| `SystemLogEvent` | Log de acciones del usuario (módulo, acción, IP) | `SystemLogEventConsumer` |
| `DataChangedEvent` | Auditoría de cambios en datos (INSERT/UPDATE/DELETE) | `DataChangedEventConsumer` |
| `UserApprovedEvent` | Evento específico de aprobación de usuario | — |

### Colas RabbitMQ

| Cola | Consumidor | Exchange |
|---|---|---|
| `audit-system-logs-queue` | `SystemLogEventConsumer` | MassTransit default |
| `audit-data-changes-queue` | `DataChangedEventConsumer` | MassTransit default |

### Consumers del Audit Worker

**SystemLogEventConsumer** — Persiste en `auditoria.t_logs_eventos`:
- `id_usuario`, `nombre_usuario`, `accion`, `modulo`, `detalles` (JSONB), `ip_origen`, `fecha_hora`

**DataChangedEventConsumer** — Persiste en `auditoria.t_auditoria_datos`:
- `fecha_hora`, `id_usuario`, `tabla_afectada`, `registro_id`, `tipo_operacion`, `valores_anteriores` (JSONB), `valores_nuevos` (JSONB)

## Base de Datos — Esquema IAM

El módulo IAM (`iam` schema en PostgreSQL):

- **t_usuarios** — Usuarios del sistema (GUID PK, email único, password hash, estado, aprobación)
- **t_personas** — Datos personales (nombre, apellido, documento, teléfono)
- **t_roles** — Roles del sistema
- **t_modulos** — Módulos de la aplicación
- **t_roles_modulos** — Permisos (roles → módulos)
- **t_organizaciones** — Organizaciones/entidades
- **t_jurisdicciones_usuarios** — Vinculación usuario ↔ organización (Gestor Licitación)
- **t_estados_usuario** — Catálogo de estados (Activo, Pendiente, Inactivo)
- **t_tipos_documento** — Catálogo tipos de documento
- **t_tipos_persona** — Catálogo tipos de persona

Esquema de negocio (`negocio` schema):

- **t_proveedores** — Proveedores (Razón Social, CUIT)
- **t_proveedores_representantes** — Vinculación usuario ↔ proveedor (Proveedor Directo)
- **t_rubros** — Rubros de proveedores
- **t_proveedores_rubros** — Proveedores por rubro

## Estructura del Proyecto Identity

```
PortalSubastas.Identity/
├── PortalSubastas.Identity.API/           # Web API
│   ├── Config/                            # DI, JWT, Swagger, OpenTelemetry
│   ├── Controllers/                       # Auth, User, Role, Organization, Provider
│   └── Middlewares/                       # GlobalExceptionHandlingMiddleware
├── PortalSubastas.Identity.Application/   # Capa de aplicación
│   ├── AutoMapper/                        # Profiles (UserProfile, AuthProfile, CommonProfile)
│   ├── RequestDto/                        # DTOs de entrada
│   ├── ResponseDto/                       # DTOs de salida
│   └── Services/                          # Implementaciones + BaseService
├── PortalSubastas.Identity.Domain/        # Capa de dominio
│   ├── Auditable/                         # Interfaces de auditoría
│   ├── Enums/                             # Estados
│   ├── Interceptors/                      # AuditInterceptor (EF Core)
│   └── Models/                            # Entidades + DbContext
└── tests/                                 # Tests unitarios y de integración
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

### appsettings.json (Audit.Worker)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=portal_subastas;Username=usr;Password=pass"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

## Ejecutar en Desarrollo

```bash
# 1. RabbitMQ (Docker)
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management

# 2. Identity API
cd PortalSubastas.Identity/PortalSubastas.Identity.API
dotnet run

# 3. Audit Worker
cd PortalSubastas.Audit/PortalSubastas.Audit.Worker
dotnet run
```

Swagger: `http://localhost:5252/swagger`
RabbitMQ Management: `http://localhost:15672` (guest/guest)
