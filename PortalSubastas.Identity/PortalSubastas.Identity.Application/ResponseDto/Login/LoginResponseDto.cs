namespace PortalSubastas.Identity.Application.ResponseDto.Login;
using PortalSubastas.Identity.Application.ResponseDto.Role;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public List<ModuloDto> Modulos { get; set; } = new();
    public List<PaginaDto> Paginas { get; set; } = new();
    public List<EntidadDto> Entidades { get; set; } = new();
}

// NUEVA CLASE
public class EntidadDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty; // "GESTOR" o "PROVEEDOR"
    public string Nombre { get; set; } = string.Empty;
}