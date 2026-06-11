namespace PortalSubastas.Identity.Application.RequestDto.Login;

public class ConfirmEmailRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}
