namespace PortalSubastas.Identity.Application.ResponseDto.Login;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public List<ModuloDto> Modulos { get; set; } = new();
}
