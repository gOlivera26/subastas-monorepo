# Módulo IAM — PortalSubastas.Identity

> Spec: `iam-module`
> Versión: 1.0.0
> Tags: `iam`, `identity`, `auth`, `audit`, `rabbitmq`, `mass-transit`

---

## 1. Arquitectura del Módulo IAM

- **Type**: `Architecture`
- **Order**: 1

**Description**: Diagrama de containers del módulo IAM mostrando la Identity API, el Audit Worker, RabbitMQ, y las bases de datos. El usuario interactúa con la Identity API que publica eventos a RabbitMQ; el Audit Worker los consume y persiste en las tablas de auditoría.

```mermaid
C4Context
    title System Context — PortalSubastas IAM

    Person(user, "Usuario", "Operador del sistema de subastas")

    System_Ext(browser, "Browser / Frontend", "Angular SPA")

    Boundary(iam, "Módulo IAM") {
        System(identityApi, "Identity.API", "ASP.NET Core 8\nAuth JWT, CRUD usuarios,\nroles, organizaciones")
        System(auditWorker, "Audit.Worker", ".NET 8 Worker\nConsume eventos\nde auditoría")
    }

    System_Ext(rabbitmq, "RabbitMQ", "Message Broker\nMassTransit events")

    System_Ext(dbIdentity, "PostgreSQL\niam.* schema", "Datos de usuarios,\nroles, permisos")
    System_Ext(dbAudit, "PostgreSQL\nauditoria.* schema", "Logs de eventos\ny cambios de datos")

    Rel(user, browser, "HTTP", "HTTPS")
    Rel(browser, identityApi, "API REST", "JSON")
    Rel(identityApi, rabbitmq, "publish()", "SystemLogEvent\nDataChangedEvent\nUserApprovedEvent")
    Rel(rabbitmq, auditWorker, "consume", "audit-system-logs-queue\naudit-data-changes-queue")
    Rel(identityApi, dbIdentity, "EF Core", "Npgsql")
    Rel(auditWorker, dbAudit, "Npgsql", "INSERT directo")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

---

## 2. Flujo de Registro y Aprobación de Usuario

- **Type**: `Sequence`
- **Order**: 2

**Description**: Secuencia completa desde que un usuario se registra hasta que un administrador lo aprueba. Incluye validaciones, persistencia, publicación de eventos de auditoría y notificación vía RabbitMQ.

```mermaid
sequenceDiagram
    participant U as Usuario
    participant F as Frontend
    participant API as Identity.API
    participant DB as PostgreSQL
    participant RMQ as RabbitMQ
    participant W as Audit.Worker

    U->>F: Completa formulario de registro
    F->>API: POST /api/auth/register
    API->>DB: Verificar email único
    API->>DB: Verificar documento único
    API->>DB: INSERT t_personas
    API->>DB: INSERT t_usuarios (estado: PENDIENTE)
    alt Tiene IdOrganizacion
        API->>DB: INSERT t_jurisdicciones_usuarios
    else Tiene IdProveedor
        API->>DB: INSERT t_proveedores_representantes
    end
    API-->>F: 200 OK (usuario pendiente)
    F-->>U: "Registro exitoso, espere aprobación"

    Note over API,RMQ: El interceptor de EF Core dispara DataChangedEvent

    API->>RMQ: publish SystemLogEvent(REGISTRO)
    API->>RMQ: publish DataChangedEvent(t_usuarios, INSERT)

    RMQ->>W: consume audit-system-logs-queue
    RMQ->>W: consume audit-data-changes-queue
    W->>DB: INSERT auditoria.t_logs_eventos
    W->>DB: INSERT auditoria.t_auditoria_datos

    Note over F,API: Tiempo después — un admin aprueba

    actor Admin as Administrador
    Admin->>F: Aprueba usuario pendiente
    F->>API: POST /api/user/{id}/approve
    API->>DB: UPDATE t_usuarios (estado: ACTIVO, fecha_aprobacion, aprobado_por)
    API->>RMQ: publish SystemLogEvent(USUARIO_APROBADO)
    API->>RMQ: publish DataChangedEvent(t_usuarios, UPDATE)
    API-->>F: 200 OK
    F-->>Admin: "Usuario aprobado"
```

---

## 3. Modelo de Datos IAM

- **Type**: `Er`
- **Order**: 3

**Description**: Diagrama entidad-relación del esquema `iam` en PostgreSQL. Muestra las tablas principales, sus relaciones y los campos clave. El modelo soporta usuarios con dos tipos de vinculación: Gestor Licitación (organización) y Proveedor Directo (proveedor).

```mermaid
erDiagram
    t_usuarios ||--o{ t_jurisdicciones_usuarios : "tiene"
    t_usuarios ||--o{ t_proveedores_representantes : "tiene (vía persona)"
    t_usuarios }o--|| t_estados_usuario : "tiene estado"
    t_usuarios }o--|| t_roles : "tiene rol"
    t_usuarios ||--|| t_personas : "tiene datos personales"
    t_usuarios ||--o| t_usuarios : "aprobado por"

    t_roles ||--o{ t_roles_modulos : "tiene permisos"
    t_modulos ||--o{ t_roles_modulos : "asignado a"
    t_roles ||--o{ t_usuarios : "agrupa"

    t_jurisdicciones_usuarios }o--|| t_organizaciones : "pertenece a"

    t_personas ||--o{ t_proveedores_representantes : "es representante"
    t_proveedores ||--o{ t_proveedores_representantes : "tiene representante"

    t_personas }o--|| t_tipos_documento : "tipo de documento"
    t_personas }o--|| t_tipos_persona : "tipo de persona"

    t_usuarios {
        guid id PK
        int id_persona FK
        int id_rol FK
        int id_estado FK
        string email_login UK
        string password_hash
        datetime ultimo_acceso
        datetime fecha_aprobacion
        guid aprobado_por FK
        string usr_ing
        datetime fec_ing
    }

    t_personas {
        int id PK
        string nombre
        string apellido
        string nro_documento UK
        string telefono
        string email_contacto
        int id_tipo_documento FK
        int id_tipo_persona FK
    }

    t_roles {
        int id PK
        string nombre UK
        string descripcion
    }

    t_organizaciones {
        int id_organizacion PK
        string nombre
        string cuit UK
        string abreviatura
        bool activo
    }

    t_proveedores {
        int id PK
        string razon_social
        string cuit UK
        string cup
        string email_institucional
    }

    t_jurisdicciones_usuarios {
        int id_jurisdiccion_usuario PK
        guid id_usuario FK
        int id_organizacion FK
        datetime fec_asignacion
        bool es_principal
    }

    t_proveedores_representantes {
        int id_proveedor PK
        int id_persona PK
        bool es_apoderado
    }

    t_modulos {
        int id PK
        string key_name UK
        string titulo
        string descripcion
        string icono_lucide
        string ruta_frontend
        int orden
    }

    t_roles_modulos {
        int id_rol PK
        int id_modulo PK
    }
```

---

## 4. Pipeline de Auditoría con Eventos

- **Type**: `Flowchart`
- **Order**: 4

**Description**: Flujo completo de auditoría desde que se ejecuta una acción en la API hasta que se persiste en las tablas de auditoría. Muestra cómo el AuditInterceptor de EF Core captura cambios, cómo se publican los eventos, y cómo el Audit.Worker los consume y persiste.

```mermaid
flowchart TD
    subgraph "Identity.API"
        Controller["Controller\n(Action execute)"]
        Service["Service\n(Logica de negocio)"]
        DbContext["EF Core DbContext"]
        Interceptor["AuditInterceptor\n(SavingChangesAsync)"]
        Publisher["PublishEndpoint\n(MassTransit)"]

        Controller --> Service
        Service --> DbContext
        DbContext -.-> Interceptor
    end

    subgraph "RabbitMQ"
        Exchange["MassTransit Exchange"]
        LogQueue["audit-system-logs-queue"]
        DataQueue["audit-data-changes-queue"]
        Exchange --> LogQueue
        Exchange --> DataQueue
    end

    subgraph "Audit.Worker"
        LogConsumer["SystemLogEventConsumer"]
        DataConsumer["DataChangedEventConsumer"]
        LogConn["Npgsql: INSERT"]
        DataConn["Npgsql: INSERT"]
        LogConsumer --> LogConn
        DataConsumer --> DataConn
    end

    subgraph "PostgreSQL"
        TLogs["auditoria.t_logs_eventos\n(id_usuario, accion, modulo,\ndetalles JSONB, ip_origen)"]
        TAudit["auditoria.t_auditoria_datos\n(tabla, registro_id, tipo_operacion,\nvalores JSONB)"]
    end

    Service -->|"PublishSystemLogAsync()\n(action, module, details)"| Publisher
    Interceptor -->|"DataChangedEvent\n(INSERT/UPDATE/DELETE)"| Publisher
    Publisher --> Exchange
    LogConn --> TLogs
    DataConn --> TAudit

    style LogConsumer fill:#4a90d9,color:#fff
    style DataConsumer fill:#4a90d9,color:#fff
    style Interceptor fill:#e6a817,color:#000
    style TLogs fill:#2d5a27,color:#fff
    style TAudit fill:#2d5a27,color:#fff
```

---

## 5. Ciclo de Vida del Usuario

- **Type**: `State`
- **Order**: 5

**Description**: Máquina de estados de un usuario dentro del sistema IAM. Desde el registro como Pendiente, pasando por la aprobación como Activo, hasta posibles estados de Inactivo o Bloqueado. Incluye las acciones que disparan cada transición y los eventos de auditoría asociados.

```mermaid
stateDiagram-v2
    [*] --> Pendiente : Registro exitoso
    Pendiente --> Activo : Admin aprueba
    Pendiente --> Inactivo : Admin rechaza / Baja

    Activo --> Inactivo : Baja lógica (soft delete)
    Activo --> Bloqueado : Intentos fallidos de login
    Bloqueado --> Activo : Admin desbloquea

    Inactivo --> Activo : Admin reactiva
    Inactivo --> [*] : Purga de datos

    note right of Pendiente
        Estado por defecto al registrarse.
        Usuario no puede iniciar sesión.
        Se publica SystemLogEvent(REGISTRO)
        + DataChangedEvent(t_usuarios, INSERT)
    end note

    note right of Activo
        Usuario puede operar normalmente.
        Se publica SystemLogEvent(APROBACION)
        + DataChangedEvent(t_usuarios, UPDATE)
        Se setea fecha_aprobacion y aprobado_por
    end note

    note right of Inactivo
        Soft delete (FecBaja != null).
        El Global Query Filter lo excluye.
        No puede iniciar sesión.
    end note
```

---

## 6. Flujo de Login y Generación de JWT

- **Type**: `Sequence`
- **Order**: 6

**Description**: Secuencia de autenticación desde que el usuario ingresa sus credenciales hasta que recibe el token JWT con sus módulos y roles. Incluye la verificación de estado, la validación BCrypt, y la carga de módulos autorizados.

```mermaid
sequenceDiagram
    participant U as Usuario
    participant F as Frontend
    participant API as Identity.API
    participant DB as PostgreSQL

    U->>F: Ingresa email + password
    F->>API: POST /api/auth/login (email, password)

    API->>DB: SELECT t_usuarios (Include: Persona, Rol, Estado)
    DB-->>API: Usuario (o null)

    alt Usuario == null
        API-->>F: 401 "Credenciales incorrectas"
        F-->>U: Error de autenticación
    else Estado != ACTIVO
        API-->>F: 401 "Usuario inactivo o bloqueado"
        F-->>U: Error de cuenta
    else Password incorrecto (BCrypt Verify)
        API-->>F: 401 "Credenciales incorrectas"
        F-->>U: Error de autenticación
    else Credenciales válidas
        API->>DB: SELECT módulos del rol (vía t_roles_modulos + t_modulos)
        DB-->>API: Lista de módulos autorizados

        Note over API: Genera JWT con claims:\n- NameIdentifier (GUID)\n- Email\n- Name (Nombre + Apellido)\n- Role (Nombre del rol)

        API->>DB: UPDATE t_usuarios (ultimo_acceso = now())
        API-->>F: 200 { token, nombreUsuario, email, rol, modulos }
        F-->>U: Redirige al dashboard
    end
```

---

## Notas Técnicas

- La Identity API usa **GlobalExceptionHandlingMiddleware** — no hay try-catch en los servicios, las excepciones fluyen hacia el middleware que responde con `OperationResponse` estandarizado.
- Los **AutoMapper Profiles** centralizan todo el mapeo: `UserProfile.cs`, `AuthProfile.cs`, `CommonProfile.cs`.
- El **AuditInterceptor** de EF Core captura automáticamente todos los cambios (INSERT, UPDATE, DELETE) y publica `DataChangedEvent` sin necesidad de código manual en cada servicio.
- **Soft Delete**: todas las entidades que implementan `IFullAuditableEntity` tienen query filter global (`FecBaja == null`).
- Las contraseñas se hashean con **BCrypt** (nunca en texto plano).
- El token JWT tiene los claims: `NameIdentifier` (GUID), `Email`, `Name`, `Role`.

### Auditoría (AuditInterceptor + PublishSystemLogAsync)

- **AuditInterceptor**: Interceptor de EF Core que captura automáticamente todos los cambios (INSERT, UPDATE, DELETE) y publica `DataChangedEvent` vía MassTransit sin código manual en cada servicio.
- **PublishSystemLogAsync**: Método en `BaseService` que publica `SystemLogEvent` con el módulo `"IAM"`, la acción, y detalles JSONB.

**Acciones de auditoría registradas (8):**

| Acción | Servicio | Trigger |
|--------|----------|---------|
| `INICIO_SESION` | AuthService | Login exitoso |
| `NUEVO_REGISTRO` | AuthService | Registro de usuario |
| `CAMBIO_PASSWORD` | AuthService | Cambio de contraseña |
| `USUARIO_APROBADO` | UserService | Admin aprueba usuario |
| `RESETEO_PASSWORD` | UserService | Admin resetea contraseña |
| `USUARIO_DESVINCULADO` | UserService | Desvincular entidad (Gestor/Proveedor) |
| `CAMBIO_ROL` | UserService | Cambio de rol de usuario |
| `USUARIO_VINCULADO` | UserService | Vincular entidad (Gestor/Proveedor) |
