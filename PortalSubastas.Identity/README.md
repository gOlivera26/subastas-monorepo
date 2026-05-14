# .NET 8 Backend Template

Template profesional para proyectos ASP.NET Core Web API con arquitectura limpia.

## Caracteristicas

- **.NET 8.0** - Ultima version LTS
- **Arquitectura Limpia** - Separacion en capas (Domain, Application, API)
- **OperationResponse<T>** - Patron Builder para respuestas estandarizadas
- **BaseService** - Servicio base con auditoria completa y Soft Delete
- **Entity Framework Core** - ORM con PostgreSQL
- **FluentValidation** - Validacion de DTOs
- **JWT Authentication** - Seguridad con JSON Web Tokens
- **Swagger/OpenAPI** - Documentacion automatica
- **Global Usings** - Codigo limpio sin repeticion de usings
- **Docker & Docker Compose** - Despliegue containerizado
- **Observabilidad** - OpenTelemetry con Grafana, Prometheus, Loki y Tempo
- **Testing** - xUnit con ejemplos Unit e Integration Tests

## Estructura del Proyecto

```
src/
├── PortalSubastas.Identity.Domain/         # Entidades e interfaces
├── PortalSubastas.Identity.Application/    # Logica de negocio, Servicios, DTOs
└── PortalSubastas.Identity.API/           # Web API, Controladores, Middlewares
```

## Instalacion Rapida

### 1. Clonar el repositorio

```bash
git clone https://github.com/TU_USUARIO/dotnet-8-backend-template.git
cd dotnet-8-backend-template
```

### 2. Renombrar el proyecto

```powershell
# En Windows PowerShell
.\Rename-Template.ps1 -NewName MiNuevoProyecto
```

Este script renombrara:
- Carpetas (PortalSubastas.Identity.API -> MiNuevoProyecto.API)
- Archivos (.csproj, .sln, etc.)
- Namespaces en todo el codigo
- Al final pregunta si desea actualizar Docker y Observabilidad

Tambien puedes ejecutar el script de Docker por separado:

```powershell
# Solo actualizar configuracion Docker/Observabilidad
.\Update-DockerConfig.ps1 -NewName MiNuevoProyecto
```

### 3. Ejecutar

```bash
dotnet build
dotnet run
```

Swagger estara disponible en: `http://localhost:5252/swagger`

## Configuracion

### Base de Datos

Edita `appsettings.json` para configurar la conexion a PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MiDB;Username=usr;Password=pass"
  }
}
```

### JWT

Edita la configuracion del token en `appsettings.json`:

```json
{
  "Jwt": {
    "Issuer": "MiProyecto",
    "Audience": "MiProyecto", 
    "SecretKey": "TuClaveSecretaDeAlMenos32Caracteres"
  }
}
```

## Uso del BaseService

```csharp
public class MiEntidadService : BaseService
{
    public async Task<OperationResponse<MiEntidadDto>> Create(MiEntidadDto dto)
    {
        // La auditoria se maneja automaticamente
        return await InsertAsync<MiEntidad, MiEntidadDto>(dto, _context);
    }
}
```

## Entidades con Soft Delete

Para usar Soft Delete, implementa `IFullAuditableEntity`:

```csharp
public class MiEntidad : IFullAuditableEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    
    // Auditoria
    public string CreadoPor { get; set; }
    public DateTime CreadoEl { get; set; }
    public string ModificadoPor { get; set; }
    public DateTime? ModificadoEl { get; set; }
    
    // Soft Delete
    public bool IsDeleted { get; set; }
    public string EliminadoPor { get; set; }
    public DateTime? EliminadoEl { get; set; }
    public string MotivoBaja { get; set; }
}
```

## Testing

El template incluye un proyecto de testing con **xUnit** listo para usar.

### Estructura de Tests

```
tests/
└── PortalSubastas.Identity.Tests/
    ├── Unit/
    │   └── Services/
    │       └── OperationResponseTests.cs    # Ejemplo de tests unitarios
    └── Integration/
        └── Controllers/
            └── IntegrationTestBase.cs      # Base para tests de integracion
```

### Paquetes Incluidos

- **xUnit** - Framework de testing
- **FluentAssertions** - Assertions mas legibles
- **Moq** - Para mocking
- **Microsoft.AspNetCore.Mvc.Testing** - Tests de integracion
- **Microsoft.EntityFrameworkCore.InMemory** - Base de datos en memoria
- **coverlet.collector** - Coverage de codigo

### Ejecutar Tests

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar con coverage
dotnet test --collect:"XPlat Code Coverage"

# Ejecutar solo tests unitarios
dotnet test --filter "Category=Unit"

# Ejecutar solo tests de integracion
dotnet test --filter "Category=Integration"
```

### Agregar un Test Unitario

```csharp
[Fact]
public void MiMetodo_ConParametro_DeberiaRetornarResultado()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = MiClase.MiMetodo(input);
    
    // Assert
    result.Should().NotBeNull();
    result.Should().Be("expected");
}
```

### Agregar un Test de Integracion

```csharp
public class MiControllerTests : IntegrationTestBase
{
    private readonly HttpClient _client;
    
    public MiControllerTests()
    {
        _client = CreateClient();
    }
    
    [Fact]
    public async Task GetAll_DeberiaRetornar200()
    {
        var response = await _client.GetAsync("/api/mientidad");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Docker

### Build y Run con Docker Compose

### Build y Run con Docker Compose

```bash
docker-compose up --build
```

Esto iniciara todos los servicios:
- **API**: http://localhost:8080
- **Swagger**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/health
- **PostgreSQL**: localhost:5432
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin)
- **Loki**: http://localhost:3100
- **Tempo**: http://localhost:3200

### Build Manual

```bash
# Build de la imagen
docker build -t PortalSubastas.Identity-api ./PortalSubastas.Identity.API

# Run del contenedor
docker run -p 8080:8080 PortalSubastas.Identity-api
```

## Observabilidad

El template incluye integracion completa con **OpenTelemetry** para metricas, trazas y logs.

### Métricas (Prometheus)

Las metricas estaran disponibles en `/metrics`:
- metricas HTTP requests
- metricas de runtime (.NET)
- metricas personalizadas

Acceso: `http://localhost:8080/metrics`

### Trazas (Tempo)

Las trazas se envian automaticamente a Tempo via OTLP (gRPC) en el puerto `4317`.

### Logs (Loki)

Los logs se configuran automaticamente via OTLP. Busca en Grafana con el datasource Loki.

### Configuracion de OpenTelemetry

Las variables de entorno configuradas en docker-compose:

```yaml
environment:
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://tempo:4317
  - OTEL_EXPORTER_OTLP_LOGS_ENDPOINT=http://loki:3100/otlp/v1/logs
  - OTEL_SERVICE_NAME=PortalSubastas.Identity-API
```

Para desarrollo local sin Docker, las metricas se mostraran en consola.

### Dashboards Recomendados en Grafana

1. **HTTP Metrics** - Requests, latencia, errores
2. **Runtime Metrics** - GC, memoria, threads
3. **Distributed Tracing** - Trazas de requests

## Licencia

MIT License - Sientete libre de usar este template en tus proyectos.
