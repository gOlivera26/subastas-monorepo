#nullable disable
using PortalSubastas.Licitaciones.Domain.Auditable;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TGanador : IFullAuditableEntity
{
    public int IdGanador { get; set; }
    public int IdCotizacion { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int IdProveedor { get; set; }
    public decimal MontoGanador { get; set; }
    public decimal CantidadAdjudicada { get; set; }

    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCotizacion IdCotizacionNavigation { get; set; }
    public virtual TCotizacionDetalle IdCotizacionDetalleNavigation { get; set; }
    public virtual TCotizacionRenglon IdRenglonNavigation { get; set; }
}
