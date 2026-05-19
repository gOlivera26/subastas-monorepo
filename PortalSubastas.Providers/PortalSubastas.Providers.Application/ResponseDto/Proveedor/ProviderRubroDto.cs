namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class ProviderRubroDto
{
    public int IdRubro { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int? IdRubroPadre { get; set; }
}
