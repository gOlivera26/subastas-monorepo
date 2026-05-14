namespace PortalSubastas.Identity.Application.ResponseDto.Users
{
    public class ActiveUserDto
    {
        public Guid IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty;
        public string EntidadRepresentada { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}
