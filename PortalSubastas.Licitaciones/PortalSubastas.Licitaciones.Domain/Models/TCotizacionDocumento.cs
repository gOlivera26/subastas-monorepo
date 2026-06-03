namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TCotizacionDocumento : IFullAuditableEntity
{
    public int IdCotDocumento { get; set; }
    public int IdCotizacion { get; set; }
    public string TipoDocumento { get; set; }
    public string NombreArchivo { get; set; }
    public string UrlArchivo { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCotizacion IdCotizacionNavigation { get; set; }
}
