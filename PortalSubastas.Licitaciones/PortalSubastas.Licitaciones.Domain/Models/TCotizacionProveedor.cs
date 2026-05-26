#nullable disable
using PortalSubastas.Licitaciones.Domain.Auditable;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TCotizacionProveedor : IFullAuditableEntity
{
    public int IdCotizacionProveedor { get; set; }
    public int IdCotizacion { get; set; }
    public int IdProveedor { get; set; }
    public string Ganadora { get; set; } // E = Ganador, N = No Ganador, D = Descartado

    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCotizacion IdCotizacionNavigation { get; set; }
}
