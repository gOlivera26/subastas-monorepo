namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class RubroSearchResultDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool HasChildren { get; set; }
    public int Level { get; set; }
}
