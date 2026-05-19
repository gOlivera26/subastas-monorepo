namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class RubroDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int? IdRubroPadre { get; set; }
}
