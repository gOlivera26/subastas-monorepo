namespace PortalSubastas.Identity.Application.ResponseDto.CatalogoBien;

public class CatalogoBienResponseDto
{
    public int IdItem { get; set; }
    public int? IdItemRel { get; set; }
    public string Codigo { get; set; }
    public string NItem { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? IdObjetoGasto { get; set; }
    public string OrganizacionNombre { get; set; }
    public string VigenciaEjercicio { get; set; }
    public string ObjetoGastoNombre { get; set; }
}
