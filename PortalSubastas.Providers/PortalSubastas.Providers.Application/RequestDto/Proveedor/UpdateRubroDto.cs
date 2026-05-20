namespace PortalSubastas.Providers.Application.RequestDto.Proveedor;

public class UpdateRubroDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int? IdRubroPadre { get; set; }
    public bool Imputable { get; set; }
}
