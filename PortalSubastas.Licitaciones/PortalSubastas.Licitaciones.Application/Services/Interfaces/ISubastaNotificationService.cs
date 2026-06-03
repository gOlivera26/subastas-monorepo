namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface ISubastaNotificationService
{
    Task NotificarNuevaOfertaAsync(int idCotizacion, int idOfertaSubasta, int? idCotizacionDetalle, int? idRenglon, decimal monto, int idProveedor, DateTime fechaOferta);
    Task NotificarProrrogaAsync(int idCotizacion, DateTime nuevaFechaFin);
    Task NotificarNuevaPreguntaAsync(int idCotizacion, object pregunta);
    Task NotificarNuevaRespuestaAsync(int idCotizacion, object respuesta);
    Task NotificarCierrePorTopeAsync(int idCotizacion);
}