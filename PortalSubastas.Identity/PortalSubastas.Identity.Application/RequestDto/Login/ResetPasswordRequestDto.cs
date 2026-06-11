namespace PortalSubastas.Identity.Application.RequestDto.Login;

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string NuevaPassword { get; set; } = string.Empty;
}
