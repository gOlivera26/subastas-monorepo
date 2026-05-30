#nullable disable
using PortalSubastas.Licitaciones.Domain.Auditable;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TGarantiaSubasta : IFullAuditableEntity
{
    public int IdGarantia { get; set; }
    public int IdCotizacion { get; set; }
    public int IdProveedor { get; set; }
    public int IdTipoDocumento { get; set; }
    public int IdMoneda { get; set; }
    public string CompaniaAseguradora { get; set; }
    public string NroPoliza { get; set; }
    public decimal? MontoCaucion { get; set; }
    public decimal? MontoPagare { get; set; }
    public DateOnly? FechaPagare { get; set; }
    public string Observacion { get; set; }
    public string UrlArchivo { get; set; }
    public string NombreArchivo { get; set; }

    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCotizacion IdCotizacionNavigation { get; set; }
}