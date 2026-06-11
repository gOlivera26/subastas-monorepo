namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TDocumentoItemProveedor : IFullAuditableEntity
{
    public int IdDocItem { get; set; }
    public int IdCotizacion { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int IdProveedor { get; set; }
    public string NombreArchivo { get; set; }
    public string UrlArchivo { get; set; }
    public bool Enviado { get; set; }
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