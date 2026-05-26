#nullable disable
using PortalSubastas.Licitaciones.Domain.Auditable;

namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TCotizacionEspecificacion : IFullAuditableEntity
{
    public int IdCotEspecificacion { get; set; }
    public int IdCotizacion { get; set; }
    
    public DateTime? FechaInicioSubasta { get; set; }
    public DateTime? FechaFinalizacionSubasta { get; set; }
    public DateTime? FechaLimiteConsultas { get; set; }
    public string NroExpediente { get; set; }
    
    public decimal? MargenMejora { get; set; }
    public int? CriterioAdjudicacion { get; set; } // 0 = Item, 1 = Renglon
    public bool PermiteProrroga { get; set; }
    public int? ProrrogaMinutos { get; set; }
    public string Redeterminacion { get; set; } // 1=Publica, 0=Privada, 2=Cerrada

    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCotizacion IdCotizacionNavigation { get; set; }
}
