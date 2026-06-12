namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class ObservacionesProveedoresReportResponseDto
{
    public ObservacionesProveedoresCabeceraResponseDto Cabecera { get; set; } = new();
    public List<ObservacionProveedorResponseDto> Observaciones { get; set; } = new();
}

public sealed class ObservacionesProveedoresCabeceraResponseDto
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
}

public sealed class ObservacionProveedorResponseDto
{
    public int? IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
}
