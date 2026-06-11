using PortalSubastas.Reporting.Application.Services.Interfaces;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace PortalSubastas.Reporting.Application.Services.Implementations;

public sealed class ReportDataService : IReportDataService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReportDataService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ReporteLicitacion> GetLicitacionAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/licitaciones/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos del reporte. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<ReporteLicitacionIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para reportería.");
        }

        return operation.Data.ToDomain();
    }

    public async Task<ActaPrelacionReport> GetActaPrelacionAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/acta-prelacion/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos del acta. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<ActaPrelacionIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para el acta.");
        }

        return operation.Data.ToDomain();
    }

    public async Task<DetalleSubastaReport> GetDetalleSubastaAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/detalle-subasta/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos del detalle de subasta. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<DetalleSubastaIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para el detalle de subasta.");
        }

        return operation.Data.ToDomain();
    }

    public async Task<ProveedoresInvitadosReport> GetProveedoresInvitadosAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/proveedores-invitados/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos de proveedores invitados. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<ProveedoresInvitadosIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para proveedores invitados.");
        }

        return operation.Data.ToDomain();
    }

    public async Task<PreguntasRespuestasReport> GetPreguntasRespuestasAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/preguntas-respuestas/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos de preguntas y respuestas. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<PreguntasRespuestasIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para preguntas y respuestas.");
        }

        return operation.Data.ToDomain();
    }

    public async Task<DesistimientoReport> GetDesistimientoAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/desistimiento/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos de desistimiento. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<DesistimientoIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para desistimiento.");
        }

        return operation.Data.ToDomain();
    }

    public async Task<ObservacionesProveedoresReport> GetObservacionesProveedoresAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/observaciones-proveedores/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos de observaciones de proveedores. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<ObservacionesProveedoresIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para observaciones de proveedores.");
        }

        return operation.Data.ToDomain();
    }

    public async Task<AuditoriaSubastaReport> GetAuditoriaSubastaAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/auditoria-subasta/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos de auditoria de subasta. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<AuditoriaSubastaIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para auditoria de subasta.");
        }

        return operation.Data.ToDomain();
    }


    public async Task<VerificacionDocumentacionReport> GetVerificacionDocumentacionAsync(int idCotizacion, CancellationToken cancellationToken = default)
    {
        if (idCotizacion <= 0)
        {
            throw new ArgumentException("El identificador de cotizacion debe ser mayor a cero.", nameof(idCotizacion));
        }

        PropagateAuthorizationHeader();

        var response = await _httpClient.GetAsync(
            $"internal/reporting/verificacion-documentacion/{idCotizacion}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Licitaciones no pudo devolver los datos de verificacion de documentacion. Status: {(int)response.StatusCode}. Detalle: {error}");
        }

        var operation = await response.Content.ReadFromJsonAsync<OperationResponse<VerificacionDocumentacionIntegrationDto>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
        {
            throw new InvalidOperationException(operation?.Message ?? "Licitaciones devolvio una respuesta invalida para verificacion de documentacion.");
        }

        return operation.Data.ToDomain();
    }

    private void PropagateAuthorizationHeader()
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrWhiteSpace(authorization) &&
            AuthenticationHeaderValue.TryParse(authorization, out var headerValue))
        {
            _httpClient.DefaultRequestHeaders.Authorization = headerValue;
        }
    }

    private sealed class ReporteLicitacionIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTimeOffset FechaEmision { get; set; }
        public List<ReporteLicitacionRenglonIntegrationDto> Renglones { get; set; } = new();

        public ReporteLicitacion ToDomain()
        {
            return new ReporteLicitacion(
                IdCotizacion,
                Numero,
                Titulo,
                Estado,
                FechaEmision,
                Renglones.Select(r => new ReporteLicitacionRenglon(
                    r.Numero,
                    r.Descripcion,
                    r.Cantidad,
                    r.UnidadMedida,
                    r.PrecioEstimado)).ToList());
        }
    }

    private sealed class ReporteLicitacionRenglonIntegrationDto
    {
        public int Numero { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal PrecioEstimado { get; set; }
    }

    private sealed class ActaPrelacionIntegrationDto
    {
        public ActaPrelacionCabeceraIntegrationDto Cabecera { get; set; } = new();
        public List<ActaPrelacionDetalleIntegrationDto> Detalles { get; set; } = new();
        public List<ActaPrelacionOfertaIntegrationDto> OfertasIniciales { get; set; } = new();
        public List<ActaPrelacionOfertaIntegrationDto> HistorialOfertas { get; set; } = new();
        public List<ActaPrelacionGanadorIntegrationDto> Ganadores { get; set; } = new();

        public ActaPrelacionReport ToDomain()
        {
            return new ActaPrelacionReport(
                Cabecera.ToDomain(),
                Detalles.Select(d => d.ToDomain()).ToList(),
                OfertasIniciales.Select(o => o.ToDomain()).ToList(),
                HistorialOfertas.Select(o => o.ToDomain()).ToList(),
                Ganadores.Select(g => g.ToDomain()).ToList());
        }
    }

    private sealed class ActaPrelacionCabeceraIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoContratacion { get; set; } = string.Empty;
        public string CriterioAdjudicacion { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string? NumeroExpediente { get; set; }
        public decimal? MargenMejora { get; set; }
        public DateTime? FechaInicioSubasta { get; set; }
        public DateTime? FechaFinalizacionSubasta { get; set; }
        public DateTimeOffset FechaEmision { get; set; }

        public ActaPrelacionCabecera ToDomain()
        {
            return new ActaPrelacionCabecera(
                IdCotizacion,
                NumeroCotizacion,
                Titulo,
                Estado,
                TipoContratacion,
                CriterioAdjudicacion,
                UnidadAdministrativa,
                NumeroExpediente,
                MargenMejora,
                FechaInicioSubasta,
                FechaFinalizacionSubasta,
                FechaEmision);
        }
    }

    private sealed class ActaPrelacionDetalleIntegrationDto
    {
        public int IdCotizacionDetalle { get; set; }
        public int? IdRenglon { get; set; }
        public int Numero { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioBase { get; set; }
        public decimal TotalBase { get; set; }

        public ActaPrelacionDetalle ToDomain() =>
            new(IdCotizacionDetalle, IdRenglon, Numero, Descripcion, Cantidad, PrecioBase, TotalBase);
    }

    private sealed class ActaPrelacionOfertaIntegrationDto
    {
        public int IdOfertaSubasta { get; set; }
        public int IdProveedor { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string? Cuit { get; set; }
        public int? IdCotizacionDetalle { get; set; }
        public int? IdRenglon { get; set; }
        public int NumeroDetalle { get; set; }
        public string Detalle { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal Monto { get; set; }
        public decimal Total { get; set; }
        public DateTime FechaOferta { get; set; }
        public int NumeroOferta { get; set; }
        public bool EsOfertaInicial { get; set; }

        public ActaPrelacionOferta ToDomain() =>
            new(IdOfertaSubasta, IdProveedor, Proveedor, Cuit, IdCotizacionDetalle, IdRenglon, NumeroDetalle, Detalle, Cantidad, Monto, Total, FechaOferta, NumeroOferta, EsOfertaInicial);
    }

    private sealed class ActaPrelacionGanadorIntegrationDto
    {
        public int IdProveedor { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string? Cuit { get; set; }
        public int? IdCotizacionDetalle { get; set; }
        public int? IdRenglon { get; set; }
        public int NumeroDetalle { get; set; }
        public string Detalle { get; set; } = string.Empty;
        public decimal MontoGanador { get; set; }
        public decimal CantidadAdjudicada { get; set; }

        public ActaPrelacionGanador ToDomain() =>
            new(IdProveedor, Proveedor, Cuit, IdCotizacionDetalle, IdRenglon, NumeroDetalle, Detalle, MontoGanador, CantidadAdjudicada);
    }

    private sealed class DetalleSubastaIntegrationDto
    {
        public DetalleSubastaCabeceraIntegrationDto Cabecera { get; set; } = new();
        public List<DetalleSubastaItemIntegrationDto> Items { get; set; } = new();
        public List<DetalleSubastaProveedorIntegrationDto> Proveedores { get; set; } = new();

        public DetalleSubastaReport ToDomain()
        {
            return new DetalleSubastaReport(
                Cabecera.ToDomain(),
                Items.Select(i => i.ToDomain()).ToList(),
                Proveedores.Select(p => p.ToDomain()).ToList());
        }
    }

    private sealed class DetalleSubastaCabeceraIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoContratacion { get; set; } = string.Empty;
        public string CriterioAdjudicacion { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string? NumeroExpediente { get; set; }
        public decimal? MargenMejora { get; set; }
        public DateTime? FechaInicioSubasta { get; set; }
        public DateTime? FechaFinalizacionSubasta { get; set; }
        public DateTimeOffset FechaEmision { get; set; }

        public DetalleSubastaCabecera ToDomain()
        {
            return new DetalleSubastaCabecera(
                IdCotizacion,
                NumeroCotizacion,
                Titulo,
                Estado,
                TipoContratacion,
                CriterioAdjudicacion,
                UnidadAdministrativa,
                NumeroExpediente,
                MargenMejora,
                FechaInicioSubasta,
                FechaFinalizacionSubasta,
                FechaEmision);
        }
    }

    private sealed class DetalleSubastaItemIntegrationDto
    {
        public int IdCotizacionDetalle { get; set; }
        public int? IdRenglon { get; set; }
        public int Numero { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal ImporteBase { get; set; }
        public decimal TotalBase { get; set; }
        public decimal? ImporteMinimo { get; set; }
        public string? Moneda { get; set; }

        public DetalleSubastaItem ToDomain() =>
            new(IdCotizacionDetalle, IdRenglon, Numero, Descripcion, Cantidad, ImporteBase, TotalBase, ImporteMinimo, Moneda);
    }

    private sealed class DetalleSubastaProveedorIntegrationDto
    {
        public int IdProveedor { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string? Cuit { get; set; }
        public string EstadoParticipacion { get; set; } = string.Empty;

        public DetalleSubastaProveedor ToDomain() =>
            new(IdProveedor, Proveedor, Cuit, EstadoParticipacion);
    }

    private sealed class ProveedoresInvitadosIntegrationDto
    {
        public ProveedoresInvitadosCabeceraIntegrationDto Cabecera { get; set; } = new();
        public List<ProveedorInvitadoIntegrationDto> Proveedores { get; set; } = new();

        public ProveedoresInvitadosReport ToDomain()
        {
            return new ProveedoresInvitadosReport(
                Cabecera.ToDomain(),
                Proveedores.Select(p => p.ToDomain()).ToList());
        }
    }

    private sealed class ProveedoresInvitadosCabeceraIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoContratacion { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string? NumeroExpediente { get; set; }
        public DateTimeOffset FechaEmision { get; set; }

        public ProveedoresInvitadosCabecera ToDomain()
        {
            return new ProveedoresInvitadosCabecera(
                IdCotizacion,
                NumeroCotizacion,
                Titulo,
                Estado,
                TipoContratacion,
                UnidadAdministrativa,
                NumeroExpediente,
                FechaEmision);
        }
    }

    private sealed class ProveedorInvitadoIntegrationDto
    {
        public int IdProveedor { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string? Cuit { get; set; }
        public string? Mail { get; set; }
        public string? Telefono { get; set; }

        public ProveedorInvitado ToDomain() =>
            new(IdProveedor, RazonSocial, Cuit, Mail, Telefono);
    }

    private sealed class PreguntasRespuestasIntegrationDto
    {
        public PreguntasRespuestasCabeceraIntegrationDto Cabecera { get; set; } = new();
        public List<PreguntaRespuestaIntegrationDto> Consultas { get; set; } = new();

        public PreguntasRespuestasReport ToDomain()
        {
            return new PreguntasRespuestasReport(
                Cabecera.ToDomain(),
                Consultas.Select(c => c.ToDomain()).ToList());
        }
    }

    private sealed class PreguntasRespuestasCabeceraIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoContratacion { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string? NumeroExpediente { get; set; }
        public DateTimeOffset FechaEmision { get; set; }

        public PreguntasRespuestasCabecera ToDomain()
        {
            return new PreguntasRespuestasCabecera(
                IdCotizacion,
                NumeroCotizacion,
                Titulo,
                Estado,
                TipoContratacion,
                UnidadAdministrativa,
                NumeroExpediente,
                FechaEmision);
        }
    }

    private sealed class PreguntaRespuestaIntegrationDto
    {
        public int IdMensaje { get; set; }
        public int? IdProveedor { get; set; }
        public string UsuarioPregunta { get; set; } = string.Empty;
        public string Pregunta { get; set; } = string.Empty;
        public DateTime FechaPregunta { get; set; }
        public string? Respuesta { get; set; }
        public string? UsuarioRespuesta { get; set; }
        public DateTime? FechaRespuesta { get; set; }

        public PreguntaRespuesta ToDomain()
        {
            return new PreguntaRespuesta(
                IdMensaje,
                IdProveedor,
                UsuarioPregunta,
                Pregunta,
                FechaPregunta,
                Respuesta,
                UsuarioRespuesta,
                FechaRespuesta);
        }
    }

    private sealed class DesistimientoIntegrationDto
    {
        public DesistimientoCabeceraIntegrationDto Cabecera { get; set; } = new();
        public string? Observaciones { get; set; }
        public string? UsuarioDesistimiento { get; set; }
        public DateTime? FechaDesistimiento { get; set; }

        public DesistimientoReport ToDomain()
        {
            return new DesistimientoReport(
                Cabecera.ToDomain(),
                Observaciones,
                UsuarioDesistimiento,
                FechaDesistimiento);
        }
    }

    private sealed class DesistimientoCabeceraIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoContratacion { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string? NumeroExpediente { get; set; }
        public DateTimeOffset FechaEmision { get; set; }

        public DesistimientoCabecera ToDomain()
        {
            return new DesistimientoCabecera(
                IdCotizacion,
                NumeroCotizacion,
                Titulo,
                Estado,
                TipoContratacion,
                UnidadAdministrativa,
                NumeroExpediente,
                FechaEmision);
        }
    }

    private sealed class ObservacionesProveedoresIntegrationDto
    {
        public ObservacionesProveedoresCabeceraIntegrationDto Cabecera { get; set; } = new();
        public List<ObservacionProveedorIntegrationDto> Observaciones { get; set; } = new();

        public ObservacionesProveedoresReport ToDomain()
        {
            return new ObservacionesProveedoresReport(
                Cabecera.ToDomain(),
                Observaciones.Select(o => o.ToDomain()).ToList());
        }
    }

    private sealed class ObservacionesProveedoresCabeceraIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoContratacion { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string? NumeroExpediente { get; set; }
        public DateTime? FechaLimiteImpugnar { get; set; }
        public DateTimeOffset FechaEmision { get; set; }

        public ObservacionesProveedoresCabecera ToDomain()
        {
            return new ObservacionesProveedoresCabecera(
                IdCotizacion,
                NumeroCotizacion,
                Titulo,
                Estado,
                TipoContratacion,
                UnidadAdministrativa,
                NumeroExpediente,
                FechaLimiteImpugnar,
                FechaEmision);
        }
    }

    private sealed class ObservacionProveedorIntegrationDto
    {
        public int? IdProveedor { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string? Cuit { get; set; }
        public string Observacion { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;

        public ObservacionProveedor ToDomain()
        {
            return new ObservacionProveedor(IdProveedor, Proveedor, Cuit, Observacion, Origen);
        }
    }

    private sealed class AuditoriaSubastaIntegrationDto
    {
        public AuditoriaSubastaCabeceraIntegrationDto Cabecera { get; set; } = new();
        public AuditoriaSubastaResumenIntegrationDto Resumen { get; set; } = new();
        public List<AuditoriaSubastaMovimientoIntegrationDto> Movimientos { get; set; } = new();

        public AuditoriaSubastaReport ToDomain()
        {
            return new AuditoriaSubastaReport(
                Cabecera.ToDomain(),
                Resumen.ToDomain(),
                Movimientos.Select(m => m.ToDomain()).ToList());
        }
    }

    private sealed class AuditoriaSubastaCabeceraIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoContratacion { get; set; } = string.Empty;
        public string CriterioAdjudicacion { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string? NumeroExpediente { get; set; }
        public DateTime? FechaCotizacion { get; set; }
        public DateTime? FechaInicioSubasta { get; set; }
        public DateTimeOffset FechaEmision { get; set; }

        public AuditoriaSubastaCabecera ToDomain()
        {
            return new AuditoriaSubastaCabecera(
                IdCotizacion,
                NumeroCotizacion,
                Titulo,
                Estado,
                TipoContratacion,
                CriterioAdjudicacion,
                UnidadAdministrativa,
                NumeroExpediente,
                FechaCotizacion,
                FechaInicioSubasta,
                FechaEmision);
        }
    }

    private sealed class AuditoriaSubastaResumenIntegrationDto
    {
        public int CantidadDocumentos { get; set; }
        public int CantidadOfertas { get; set; }
        public int CantidadProveedores { get; set; }

        public AuditoriaSubastaResumen ToDomain()
        {
            return new AuditoriaSubastaResumen(
                CantidadDocumentos,
                CantidadOfertas,
                CantidadProveedores);
        }
    }

    private sealed class AuditoriaSubastaMovimientoIntegrationDto
    {
        public string Tipo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string? Usuario { get; set; }
        public DateTime? Fecha { get; set; }

        public AuditoriaSubastaMovimiento ToDomain()
        {
            return new AuditoriaSubastaMovimiento(
                Tipo,
                Descripcion,
                Cantidad,
                Usuario,
                Fecha);
        }
    }

    private sealed class VerificacionDocumentacionIntegrationDto
    {
        public VerificacionDocumentacionCabeceraIntegrationDto Cabecera { get; set; } = new();
        public List<VerificacionDocumentacionItemIntegrationDto> Documentos { get; set; } = new();

        public VerificacionDocumentacionReport ToDomain()
        {
            return new VerificacionDocumentacionReport(
                Cabecera.ToDomain(),
                Documentos.Select(d => d.ToDomain()).ToList());
        }
    }

    private sealed class VerificacionDocumentacionCabeceraIntegrationDto
    {
        public int IdCotizacion { get; set; }
        public string NumeroCotizacion { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TipoContratacion { get; set; } = string.Empty;
        public string CriterioAdjudicacion { get; set; } = string.Empty;
        public string UnidadAdministrativa { get; set; } = string.Empty;
        public string? NumeroExpediente { get; set; }
        public DateTime? FechaInicioSubasta { get; set; }
        public DateTime? FechaFinalizacionSubasta { get; set; }
        public DateTimeOffset FechaEmision { get; set; }
        public string NotaAdecuacion { get; set; } = string.Empty;

        public VerificacionDocumentacionCabecera ToDomain()
        {
            return new VerificacionDocumentacionCabecera(
                IdCotizacion,
                NumeroCotizacion,
                Titulo,
                Estado,
                TipoContratacion,
                CriterioAdjudicacion,
                UnidadAdministrativa,
                NumeroExpediente,
                FechaInicioSubasta,
                FechaFinalizacionSubasta,
                FechaEmision,
                NotaAdecuacion);
        }
    }

    private sealed class VerificacionDocumentacionItemIntegrationDto
    {
        public string Origen { get; set; } = string.Empty;
        public int? IdProveedor { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string? Cuit { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public string? UrlArchivo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaPresentacion { get; set; }
        public string? Observaciones { get; set; }

        public VerificacionDocumentacionItem ToDomain()
        {
            return new VerificacionDocumentacionItem(
                Origen,
                IdProveedor,
                Proveedor,
                Cuit,
                TipoDocumento,
                NombreArchivo,
                UrlArchivo,
                Estado,
                FechaPresentacion,
                Observaciones);
        }
    }

}
