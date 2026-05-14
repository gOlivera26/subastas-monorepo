using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalSubastas.Identity.Application.RequestDto.Login;

public class RegisterRequestDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NroDocumento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int IdTipoPersona { get; set; } = 1;
    public int IdTipoDocumento { get; set; } = 1;
    public int IdRol { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? IdProveedor { get; set; }
}