#nullable disable
using PortalSubastas.Identity.Domain.Auditable;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TCategoriaProgramatica : IFullAuditableEntity
{
    public int IdCatProg { get; set; }
    public int? IdCatProgRel { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? IdUnidadAdm { get; set; }
    public int IdVigencia { get; set; }
    public int Codigo { get; set; }
    public string Nombre { get; set; }
    public string Naturaleza { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCategoriaProgramatica IdCatProgRelNavigation { get; set; }
    public virtual ICollection<TCategoriaProgramatica> InverseIdCatProgRelNavigation { get; set; } = new List<TCategoriaProgramatica>();
    public virtual TOrganizacione IdOrganizacionNavigation { get; set; }
    public virtual TUnidadAdministrativa IdUnidadAdmNavigation { get; set; }
    public virtual TVigencia IdVigenciaNavigation { get; set; }
}
