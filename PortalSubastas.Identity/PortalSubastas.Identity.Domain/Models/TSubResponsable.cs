#nullable disable
using PortalSubastas.Identity.Domain.Auditable;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TSubResponsable : IFullAuditableEntity
{
    public int IdSubResponsable { get; set; }
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }
    public int? IdSubRespRel { get; set; }
    public bool Vigente { get; set; }
    public int? IdUnidadAdm { get; set; }

    public virtual TSubResponsable IdSubRespRelNavigation { get; set; }
    public virtual ICollection<TSubResponsable> InverseIdSubRespRelNavigation { get; set; } = new List<TSubResponsable>();
    public virtual TUnidadAdministrativa IdUnidadAdmNavigation { get; set; }
}
