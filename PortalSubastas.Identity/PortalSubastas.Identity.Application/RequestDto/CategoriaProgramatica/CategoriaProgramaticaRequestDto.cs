namespace PortalSubastas.Identity.Application.RequestDto.CategoriaProgramatica;

public class CategoriaProgramaticaRequestDto
{
    public int? IdCatProgRel { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? IdUnidadAdm { get; set; }
    public int IdVigencia { get; set; }
    public int Codigo { get; set; }
    public string Nombre { get; set; }
    public string Naturaleza { get; set; }
}
