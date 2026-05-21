namespace PortalSubastas.Identity.Application.RequestDto.ObjetoGasto;

public class ObjetoGastoRequestDto
{
    public int? IdObjetoGastoRel { get; set; }
    public string NumeroObjeto { get; set; }
    public string NombreObjeto { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public bool? ImputaEjecucion { get; set; }
}
