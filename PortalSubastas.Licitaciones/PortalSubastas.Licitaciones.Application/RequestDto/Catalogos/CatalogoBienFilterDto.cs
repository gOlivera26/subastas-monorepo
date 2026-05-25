namespace PortalSubastas.Licitaciones.Application.RequestDto.Catalogos;

public class CatalogoBienFilterDto
{
    public bool? Vigente { get; set; }
    public int? Jurisdiccion { get; set; }
    public int? CategoriaBien { get; set; }
}
