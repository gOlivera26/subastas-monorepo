namespace PortalSubastas.Identity.Application.RequestDto.Login;

public class ChangePasswordRequestDto
{
    public string PasswordActual { get; set; } = string.Empty;
    public string NuevaPassword { get; set; } = string.Empty;
}
