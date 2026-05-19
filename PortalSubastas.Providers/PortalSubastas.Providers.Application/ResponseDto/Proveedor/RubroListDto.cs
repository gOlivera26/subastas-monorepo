namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class RubroListDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int? IdRubroPadre { get; set; }
    public string? RubroPadre { get; set; }
    public bool Activo { get; set; }
    public bool Imputable { get; set; }
}
