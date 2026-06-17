namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class ProveedoresInvitadosReportResponseDto
{
    public ProveedoresInvitadosCabeceraResponseDto Cabecera { get; set; } = new();
    public List<ProveedorInvitadoResponseDto> Proveedores { get; set; } = new();
}

public sealed class ProveedoresInvitadosCabeceraResponseDto
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

public sealed class ProveedorInvitadoResponseDto
{
    public int IdProveedor { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public string? Mail { get; set; }
    public string? Telefono { get; set; }
}
