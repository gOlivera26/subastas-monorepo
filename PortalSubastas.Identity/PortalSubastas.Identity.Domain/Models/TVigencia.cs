#nullable disable
using PortalSubastas.Identity.Domain.Auditable;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TVigencia : IFullAuditableEntity
{
    public int IdVigencia { get; set; }
    public short Ejercicio { get; set; }
    public bool? ActivoEjecucion { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual ICollection<TUnidadAdministrativa> TUnidadesAdministrativas { get; set; } = new List<TUnidadAdministrativa>();
}
