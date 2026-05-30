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

public class CatalogoBienBulkItemDto
{
    public int IdItem { get; set; }
    public int? IdItemRel { get; set; }
    public string Codigo { get; set; }
    public string NItem { get; set; }
    public string NumeroObjeto { get; set; }
}

public class CatalogoBienBulkUploadDto
{
    public List<CatalogoBienBulkItemDto> Items { get; set; } = new();
    public int? IdOrganizacion { get; set; }
}
