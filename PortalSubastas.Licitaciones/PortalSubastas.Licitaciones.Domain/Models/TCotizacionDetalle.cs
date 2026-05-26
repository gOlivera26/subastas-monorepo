#nullable disable
using PortalSubastas.Licitaciones.Domain.Auditable;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TCotizacionDetalle : IFullAuditableEntity
{
    public int IdCotizacionDetalle { get; set; }
    public int IdCotizacion { get; set; }
    public int IdReservaDetalle { get; set; }
    public int IdItem { get; set; } // FK Catalogo Bienes
    public int? IdRenglon { get; set; }
    
    public decimal Cantidad { get; set; }
    public decimal ImporteBase { get; set; }

    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCotizacion IdCotizacionNavigation { get; set; }
    public virtual TCotizacionRenglon IdRenglonNavigation { get; set; }
    public virtual ICollection<TGanador> Ganadores { get; set; } = new List<TGanador>();
}
