namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class VerificacionDocumentacionReportResponseDto
{
    public VerificacionDocumentacionCabeceraResponseDto Cabecera { get; set; } = new();
    public List<VerificacionDocumentacionItemResponseDto> Documentos { get; set; } = new();
}

public sealed class VerificacionDocumentacionCabeceraResponseDto
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
}

public sealed class VerificacionDocumentacionItemResponseDto
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
}
