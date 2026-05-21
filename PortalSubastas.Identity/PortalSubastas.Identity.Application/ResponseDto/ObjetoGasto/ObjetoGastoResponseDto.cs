namespace PortalSubastas.Identity.Application.ResponseDto.ObjetoGasto;

public class ObjetoGastoResponseDto
{
    public int IdObjetoGasto { get; set; }
    public int? IdObjetoGastoRel { get; set; }
    public string NumeroObjeto { get; set; }
    public string NombreObjeto { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public bool? ImputaEjecucion { get; set; }
    public string OrganizacionNombre { get; set; }
    public string VigenciaEjercicio { get; set; }
}
