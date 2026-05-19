namespace PortalSubastas.Providers.Application.RequestDto.Proveedor;

public class CreateProviderDto
{
    public string RazonSocial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string? Cup { get; set; }
    public string EmailInstitucional { get; set; } = string.Empty;
    public string? EmailAlternativo { get; set; }
    public int IdTipoPersona { get; set; }
    public string? UrlConstanciaAfip { get; set; }
}
