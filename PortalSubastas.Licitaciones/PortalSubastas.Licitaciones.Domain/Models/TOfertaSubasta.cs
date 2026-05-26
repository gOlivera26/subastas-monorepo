#nullable disable
using PortalSubastas.Licitaciones.Domain.Auditable;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TOfertaSubasta : IFullAuditableEntity
{
    public int IdOfertaSubasta { get; set; }
    public int IdCotizacion { get; set; }
    public int IdProveedor { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaOferta { get; set; }

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
