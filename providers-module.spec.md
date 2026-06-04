# Módulo Providers — PortalSubastas.Providers

> Spec: `providers-module`
> Versión: 1.0.0
> Tags: `providers`, `negocio`, `rubros`, `domicilios`, `afip`, `r2`

---

## 1. Arquitectura del Módulo Providers

- **Type**: `Architecture`
- **Order**: 1

**Description**: Diagrama de containers del módulo Providers mostrando la Providers API, su conexión a PostgreSQL (schema `negocio`), integración con Cloudflare R2 para constancias AFIP, y la comunicación con el módulo IAM para verificación de CUIT.

```mermaid
C4Context
    title System Context — PortalSubastas Providers

    Person(user, "Administrador", "Gestiona proveedores del portal")

    System_Ext(browser, "Browser / Frontend", "Angular SPA")
    System_Ext(gateway, "API Gateway", "YARP reverse proxy\n/routing a microservicios")

    Boundary(providers, "Módulo Providers") {
        System(providersApi, "Providers.API", "ASP.NET Core 8\nCRUD Proveedores\nRubros, Domicilios\nConstancia AFIP")
    }

    System_Ext(identityApi, "Identity.API", "Verificación CUIT\nProveedor Directo")

    System_Ext(r2, "Cloudflare R2", "Almacenamiento\nconstancias AFIP")

    System_Ext(dbProviders, "PostgreSQL\nnegocio.* schema", "Proveedores, rubros,\ndomicilios, vinculos")

    Rel(user, browser, "HTTP", "HTTPS")
    Rel(browser, gateway, "API REST", "JSON")
    Rel(gateway, providersApi, "proxy", "/api/Provider/*\n/api/Rubro/*\n/api/Domicilio/*")
    Rel(providersApi, dbProviders, "EF Core", "Npgsql")
    Rel(providersApi, identityApi, "HTTP", "GET /api/Provider/verify/{cuit}")
    Rel(providersApi, r2, "PUT object", "constancias/{cuit}.pdf")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

---

## 2. Flujo de Creación de Proveedor

- **Type**: `Sequence`
- **Order**: 2

**Description**: Secuencia completa desde que un administrador crea un proveedor hasta que queda registrado con sus datos verificados. Incluye la verificación de CUIT contra IAM, validación de duplicados, y publicación de eventos de auditoría.

```mermaid
sequenceDiagram
    participant A as Administrador
    participant F as Frontend
    participant GW as API Gateway
    participant API as Providers.API
    participant IAM as Identity.API
    participant DB as PostgreSQL

    A->>F: Completa formulario de proveedor
    F->>GW: POST /api/Provider
    GW->>API: Forward a Providers.API

    API->>IAM: GET /api/Provider/verify/{cuit}
    IAM-->>API: 200 OK (proveedor existe en IAM)
    alt CUIT no existe en IAM
        API-->>F: 400 "CUIT no empadronado"
        F-->>A: Error de validación
    end

    API->>DB: Verificar CUIT único en t_proveedores
    API->>DB: INSERT t_proveedores
    API-->>F: 200 OK (proveedor creado)
    F-->>A: "Proveedor registrado exitosamente"

    Note over API: Se publica SystemLogEvent(PROVEEDOR_CREADO)
    Note over API: AuditInterceptor captura INSERT en t_proveedores
```

---

## 3. Modelo de Datos — Esquema Negocio

- **Type**: `Er`
- **Order**: 3

**Description**: Diagrama entidad-relación del esquema `negocio` en PostgreSQL. Muestra las tablas de proveedores, rubros jerárquicos, domicilios y sus relaciones.

```mermaid
erDiagram
    t_proveedores ||--o{ t_proveedores_rubros : "tiene"
    t_proveedores ||--o{ t_proveedores_representantes : "tiene representante"
    t_proveedores }o--|| t_tipos_persona : "tipo de persona"

    t_rubros ||--o{ t_rubros : "padre de (jerarquía)"
    t_rubros ||--o{ t_proveedores_rubros : "asignado a"

    t_personas ||--o{ t_domicilios : "tiene"
    t_domicilios }o--|| t_tipos_domicilio : "tipo"
    t_domicilios }o--|| t_provincias : "provincia"

    t_proveedores {
        int id PK
        string razon_social
        string cuit UK
        string cup
        string email_institucional
        string email_alternativo
        int id_tipo_persona FK
        string usr_ing
        datetime fec_ing
        string usr_baja
        datetime fec_baja
    }

    t_rubros {
        int id PK
        string codigo UK
        string descripcion
        int id_rubro_padre FK "null = raíz"
        bool imputable
        string usr_ing
        datetime fec_ing
        string usr_baja
        datetime fec_baja
    }

    t_proveedores_rubros {
        int id_proveedor PK
        int id_rubro PK
        datetime fecha_vinculacion
        string usr_ing
        datetime fec_ing
        string usr_baja
        datetime fec_baja
    }

    t_domicilios {
        int id PK
        int id_persona FK
        int id_tipo_domicilio FK
        string calle
        string numero
        string piso
        string departamento
        string barrio
        string ciudad
        int id_provincia FK
        string codigo_postal
        string telefono
        string fax
        string usr_ing
        datetime fec_ing
        string usr_baja
        datetime fec_baja
    }

    t_tipos_domicilio {
        int id PK
        string descripcion UK
    }

    t_provincias {
        int id PK
        string nombre UK
    }

    t_tipos_persona {
        int id PK
        string descripcion UK
    }
```

---

## 4. Jerarquía de Rubros

- **Type**: `Flowchart`
- **Order**: 4

**Description**: Estructura jerárquica de rubros con hasta 3 niveles de profundidad. Los rubros raíz no tienen padre (`id_rubro_padre = null`). Cada rubro puede tener hijos y puede ser marcado como "imputable".

```mermaid
flowchart TD
    subgraph "Nivel 1 — Raíz"
        R1["99999901 — Explotación De Minas Y Canteras"]
        R2["99999902 — Industria Manufacturera"]
    end

    subgraph "Nivel 2 — Categoría"
        R11["51 — Extracción de carbón"]
        R12["52 — Extracción de lignito"]
        R21["15 — Fabricación de alimentos"]
    end

    subgraph "Nivel 3 — Subcategoría"
        R111["51000 — Extracción de hulla"]
        R112["51001 — Aglomeración de carbón"]
    end

    R1 --> R11
    R1 --> R12
    R2 --> R21
    R11 --> R111
    R11 --> R112

    style R1 fill:#4a90d9,color:#fff
    style R2 fill:#4a90d9,color:#fff
    style R11 fill:#6ab04c,color:#fff
    style R12 fill:#6ab04c,color:#fff
    style R21 fill:#6ab04c,color:#fff
    style R111 fill:#e6a817,color:#000
    style R112 fill:#e6a817,color:#000
```

**Reglas de negocio:**
- Máximo 3 niveles de profundidad (raíz → categoría → subcategoría)
- Un rubro no puede ser su propio padre
- No se puede eliminar un rubro que tiene hijos activos
- No se puede eliminar un rubro vinculado a proveedores
- El campo `imputable` indica si el rubro genera obligaciones impositivas

---

## 5. Carga de Constancia AFIP a Cloudflare R2

- **Type**: `Sequence`
- **Order**: 5

**Description**: Flujo de subida de constancia de inscripción AFIP para un proveedor. El archivo PDF se sube a Cloudflare R2 y se almacena la URL en el registro del proveedor.

```mermaid
sequenceDiagram
    participant A as Administrador
    participant F as Frontend
    participant GW as API Gateway
    participant API as Providers.API
    participant R2 as Cloudflare R2
    participant DB as PostgreSQL

    A->>F: Selecciona archivo PDF (constancia AFIP)
    F->>GW: POST /api/Provider/{id}/constancia-afip (multipart/form-data)
    GW->>API: Forward con archivo

    API->>API: Validar extensión (.pdf)
    API->>API: Validar tamaño máximo
    API->>API: Generar nombre único: {cuit}_{timestamp}.pdf

    API->>R2: PUT object /constancias/{cuit}_{timestamp}.pdf
    R2-->>API: 200 OK (URL pública)

    API->>DB: UPDATE t_proveedores (url_constancia_afip = R2 URL)
    API-->>F: 200 OK { url: "https://r2.dev/..." }
    F-->>A: "Constancia cargada exitosamente"

    Note over API: Se publica SystemLogEvent(CONSTANCIA_AFIP_CARGADA)
```

---

## 6. Vinculación de Rubros a Proveedores

- **Type**: `Sequence`
- **Order**: 6

**Description**: Flujo para vincular uno o más rubros a un proveedor existente. Se valida que el proveedor y los rubros existan y estén activos.

```mermaid
sequenceDiagram
    participant A as Administrador
    participant F as Frontend
    participant API as Providers.API
    participant DB as PostgreSQL

    A->>F: Selecciona proveedor y rubros
    F->>API: POST /api/Provider/{id}/rubros [rubroIds]

    API->>DB: Verificar proveedor existe y activo
    API->>DB: Verificar cada rubro existe y activo
    API->>DB: Verificar no estén ya vinculados

    loop Por cada rubro
        API->>DB: INSERT t_proveedores_rubros
    end

    API-->>F: 200 OK (rubros vinculados)
    F-->>A: "Rubros vinculados exitosamente"

    Note over API: Se publica SystemLogEvent(RUBROS_VINCULADOS)
    Note over API: AuditInterceptor captura INSERTs en t_proveedores_rubros
```

---

## 7. API Endpoints

### Proveedores

| Método | Endpoint | Descripción |
|---|---|---|
| `GET` | `/api/Provider` | Listado paginado con sorting y búsqueda |
| `POST` | `/api/Provider` | Crear proveedor |
| `PUT` | `/api/Provider` | Actualizar proveedor |
| `GET` | `/api/Provider/verify/{cuit}` | Verificar CUIT (proxy a IAM) |
| `GET` | `/api/Provider/{id}/rubros` | Rubros vinculados a un proveedor |
| `POST` | `/api/Provider/{id}/rubros` | Vincular rubros a proveedor |
| `DELETE` | `/api/Provider/{id}/rubros/{rubroId}` | Desvincular rubro |
| `POST` | `/api/Provider/{id}/constancia-afip` | Subir constancia AFIP (R2) |
| `GET` | `/api/Provider/{id}/afip/verify/{cuit}` | Verificar CUIT en AFIP (mock) |

### Rubros

| Método | Endpoint | Descripción |
|---|---|---|
| `GET` | `/api/Rubro` | Listado paginado con sorting y búsqueda |
| `POST` | `/api/Rubro` | Crear rubro |
| `PUT` | `/api/Rubro` | Actualizar rubro |
| `DELETE` | `/api/Rubro/{id}` | Eliminar rubro (solo sin hijos ni proveedores) |
| `GET` | `/api/Rubro/tree` | Árbol jerárquico completo (3 niveles) |
| `GET` | `/api/Rubro/{parentId}/children` | Hijos directos de un rubro |
| `GET` | `/api/Rubro/search?q=` | Búsqueda de rubros por código/descripción |

### Domicilios

| Método | Endpoint | Descripción |
|---|---|---|
| `GET` | `/api/Domicilio/persona/{personaId}` | Domicilios de una persona |
| `POST` | `/api/Domicilio/persona/{personaId}` | Crear domicilio |
| `PUT` | `/api/Domicilio` | Actualizar domicilio |
| `DELETE` | `/api/Domicilio/{id}` | Eliminar domicilio |
| `GET` | `/api/Domicilio/tipos-domicilio` | Catálogo tipos de domicilio |
| `GET` | `/api/Domicilio/provincias` | Catálogo de provincias |

---

## Notas Técnicas

- **Paginación genérica**: Todos los listados usan `BaseService.GetPagedDataAsync<TEntity, TDto>` que centraliza `Skip/Take/Count/Map`.
- **Sorting dinámico**: Los endpoints aceptan `sortBy` y `sortDirection` como query params. El backend aplica el ordenamiento con pattern matching en LINQ.
- **Sin try-catch en servicios**: El `GlobalExceptionHandlingMiddleware` maneja todas las excepciones y responde con `OperationResponse` estandarizado.
- **Soft Delete**: Todas las entidades implementan `IFullAuditableEntity` con query filter global (`FecBaja == null`).
- **DateTimeKind.Unspecified**: Se usa `DateTime.Now` con `Kind=Unspecified` para evitar errores de timezone con PostgreSQL `timestamp without time zone`.
- **GlobalUsings**: Los namespaces comunes están centralizados en `GlobalUsings.cs` para limpiar las clases de servicio.
- **Cloudflare R2**: Se usa para almacenar constancias AFIP. La configuración va en `appsettings.json` bajo la sección `R2`.
- **AFIP Service**: Actualmente es un **mock**. La integración real con ARCA requiere certificado digital `.pfx` (ver TODO en `AfipService.cs`).
- **AutoMapper**: Los mapeos están en `RubroProfile.cs`, `ProviderProfile.cs`, `DomicilioProfile.cs`.
- **Árbol de rubros**: El endpoint `/tree` devuelve una estructura recursiva con máximo 3 niveles de profundidad.

### Auditoría (AuditInterceptor + PublishSystemLogAsync)

- **AuditInterceptor**: Interceptor de EF Core que captura automáticamente todos los cambios (INSERT, UPDATE, DELETE) y publica `DataChangedEvent` vía MassTransit sin código manual en cada servicio.
- **PublishSystemLogAsync**: Método en `BaseService` que publica `SystemLogEvent` con el módulo `"PROVIDERS"`, la acción, y detalles JSONB.

**Acciones de auditoría registradas (11):**

| Acción | Servicio | Trigger |
|--------|----------|---------|
| `PROVEEDOR_CREADO` | ProviderService | Crear proveedor |
| `PROVEEDOR_ACTUALIZADO` | ProviderService | Actualizar proveedor |
| `RUBRO_VINCULADO` | ProviderService | Vincular rubro a proveedor |
| `RUBRO_DESVINCULADO` | ProviderService | Desvincular rubro de proveedor |
| `CONSTANCIA_AFIP_SUBIDA` | ProviderService | Subir constancia AFIP a R2 |
| `RUBRO_CREADO` | RubroService | Crear rubro |
| `RUBRO_ACTUALIZADO` | RubroService | Actualizar rubro |
| `RUBRO_ELIMINADO` | RubroService | Eliminar rubro (soft delete) |
| `DOMICILIO_CREADO` | DomicilioService | Crear domicilio |
| `DOMICILIO_ACTUALIZADO` | DomicilioService | Actualizar domicilio |
| `DOMICILIO_ELIMINADO` | DomicilioService | Eliminar domicilio (soft delete) |
