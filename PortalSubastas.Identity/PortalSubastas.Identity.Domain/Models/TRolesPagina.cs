#nullable disable

namespace PortalSubastas.Identity.Domain.Models;

public partial class TRolesPagina
{
    public int IdRol { get; set; }
    public int IdPagina { get; set; }

    public virtual TRole IdRolNavigation { get; set; }
    public virtual TPagina IdPaginaNavigation { get; set; }
}
