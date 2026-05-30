namespace PortalSubastas.Identity.Application.ResponseDto.SubResponsable;

public class SubResponsableResponseDto
{
    public int IdSubResponsable { get; set; }
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public int? IdSubRespRel { get; set; }
    public bool Vigente { get; set; }
    public int? IdUnidadAdm { get; set; }
    public string UnidadAdmNombre { get; set; }
}
