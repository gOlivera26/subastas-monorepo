namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class ProviderListDto
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string Cup { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public string TipoPersona { get; set; } = string.Empty;
    public bool HasConstanciaAfip { get; set; }
    public int RubrosCount { get; set; }
    public int DomiciliosCount { get; set; }
}
