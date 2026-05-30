namespace PortalSubastas.Identity.Application.ResponseDto.CategoriaProgramatica;

public class CategoriaProgramaticaResponseDto
{
    public int IdCatProg { get; set; }
    public int? IdCatProgRel { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? IdUnidadAdm { get; set; }
    public int IdVigencia { get; set; }
    public int Codigo { get; set; }
    public string Nombre { get; set; }
    public string Naturaleza { get; set; }
    public int Nivel { get; set; }
    public bool HasChildren { get; set; }
    public string NombreJerarquia { get; set; }
    public string OrganizacionNombre { get; set; }
    public string UnidadAdmNombre { get; set; }
    public string VigenciaEjercicio { get; set; }
}
