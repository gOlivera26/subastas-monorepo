namespace PortalSubastas.Identity.Application.ResponseDto.Users;

public class UserAuditDto
{
    public DateTime? UltimoAcceso { get; set; }
    public DateTime? FechaRegistro { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTime? FechaAprobacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string ModificadoPor { get; set; } = string.Empty;
    public string AprobadoPorNombre { get; set; } = string.Empty;

}
