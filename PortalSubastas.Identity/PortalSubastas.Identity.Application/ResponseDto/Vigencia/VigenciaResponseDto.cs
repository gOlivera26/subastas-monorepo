namespace PortalSubastas.Identity.Application.ResponseDto.Vigencia;

public class VigenciaResponseDto
{
    public int IdVigencia { get; set; }
    public short Ejercicio { get; set; }
    public bool? ActivoEjecucion { get; set; }
    public DateTime? FecIng { get; set; }
}
