namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class ProviderReportLookupDto
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public string? EmailAlternativo { get; set; }
}
