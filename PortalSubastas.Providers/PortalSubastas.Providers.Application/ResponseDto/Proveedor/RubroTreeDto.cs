namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class RubroTreeDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int? IdRubroPadre { get; set; }
    public bool Imputable { get; set; }
    public bool HasChildren { get; set; }
    public List<RubroTreeDto> Children { get; set; } = new();
}
