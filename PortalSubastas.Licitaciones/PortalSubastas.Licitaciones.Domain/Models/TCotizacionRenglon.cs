#nullable disable
using PortalSubastas.Licitaciones.Domain.Auditable;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TCotizacionRenglon : IFullAuditableEntity
{
    public int IdRenglon { get; set; }
    public int IdCotizacion { get; set; }
    public int NumeroRenglon { get; set; }
    public string Descripcion { get; set; }

    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCotizacion IdCotizacionNavigation { get; set; }
    public virtual ICollection<TCotizacionDetalle> Detalles { get; set; } = new List<TCotizacionDetalle>();
}
