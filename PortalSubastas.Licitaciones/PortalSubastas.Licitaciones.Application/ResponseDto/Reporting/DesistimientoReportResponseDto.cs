namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class DesistimientoReportResponseDto
{
    public DesistimientoCabeceraResponseDto Cabecera { get; set; } = new();
    public string? Observaciones { get; set; }
    public string? UsuarioDesistimiento { get; set; }
    public DateTime? FechaDesistimiento { get; set; }
}

public sealed class DesistimientoCabeceraResponseDto
{
    public int IdCotizacion { get; set; }
    public string NumeroCotizacion { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoContratacion { get; set; } = string.Empty;
    public string UnidadAdministrativa { get; set; } = string.Empty;
    public string? NumeroExpediente { get; set; }
    public DateTimeOffset FechaEmision { get; set; }
}
