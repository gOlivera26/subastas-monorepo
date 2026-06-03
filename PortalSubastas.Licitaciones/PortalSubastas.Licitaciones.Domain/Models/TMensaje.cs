namespace PortalSubastas.Licitaciones.Domain.Models;

public partial class TMensaje
{
    public int IdMensaje { get; set; }
    public int IdCotizacion { get; set; }
    public int? IdProveedor { get; set; }
    public string Usuario { get; set; }
    public string Contenido { get; set; }
    public DateTime FecIng { get; set; }
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public string? UsuarioRespuesta { get; set; }
    public virtual TCotizacion IdCotizacionNavigation { get; set; }
}