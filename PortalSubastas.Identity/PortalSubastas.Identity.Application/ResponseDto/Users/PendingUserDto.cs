namespace PortalSubastas.Identity.Application.ResponseDto.Users;

public class PendingUserDto
{
    public Guid IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string TipoUsuario { get; set; } = string.Empty;
    public string EntidadRepresentada { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}
