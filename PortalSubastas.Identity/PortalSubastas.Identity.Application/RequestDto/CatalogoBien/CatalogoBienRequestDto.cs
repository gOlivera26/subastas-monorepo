namespace PortalSubastas.Identity.Application.RequestDto.CatalogoBien;

public class CatalogoBienRequestDto
{
    public int? IdItemRel { get; set; }
    public string Codigo { get; set; }
    public string NItem { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? IdObjetoGasto { get; set; }
}
