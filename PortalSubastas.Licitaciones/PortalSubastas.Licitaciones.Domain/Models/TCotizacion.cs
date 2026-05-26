#nullable disable
using PortalSubastas.Licitaciones.Domain.Auditable;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TCotizacion : IFullAuditableEntity
{
    public int IdCotizacion { get; set; }
    public string NroCotizacion { get; set; }
    public int IdEstado { get; set; }
    public int IdTipoContratacion { get; set; } // Ej: 7=Subasta, 9=Subasta Directa
    public int IdVigencia { get; set; }
    public int IdOrganizacion { get; set; }
    public int IdUnidadAdm { get; set; }
    public string Observacion { get; set; }

    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCotizacionEspecificacion Especificacion { get; set; }
    public virtual ICollection<TCotizacionDetalle> Detalles { get; set; } = new List<TCotizacionDetalle>();
    public virtual ICollection<TCotizacionRenglon> Renglones { get; set; } = new List<TCotizacionRenglon>();
    public virtual ICollection<TCotizacionProveedor> Proveedores { get; set; } = new List<TCotizacionProveedor>();
    public virtual ICollection<TOfertaSubasta> Ofertas { get; set; } = new List<TOfertaSubasta>();
}
