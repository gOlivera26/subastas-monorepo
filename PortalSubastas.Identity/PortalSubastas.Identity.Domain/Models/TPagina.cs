#nullable disable
using PortalSubastas.Identity.Domain.Auditable;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TPagina : IFullAuditableEntity
{
    public int Id { get; set; }
    public int IdModulo { get; set; }
    public string KeyName { get; set; }
    public string Titulo { get; set; }
    public string RutaFrontend { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TModulo IdModuloNavigation { get; set; }
    public virtual ICollection<TRolesPagina> TRolesPaginas { get; set; } = new List<TRolesPagina>();
}
