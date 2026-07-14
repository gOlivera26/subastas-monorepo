namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface ISubastaNotificationService
{
    Task NotificarNuevaOfertaAsync(int idCotizacion, int idOfertaSubasta, int? idCotizacionDetalle, int? idRenglon, decimal monto, int idProveedor, DateTime fechaOferta, string? proveedor = null, string? representante = null);
    Task NotificarMejorOfertaActualizadaAsync(int idCotizacion, int? idCotizacionDetalle, int? idRenglon, decimal mejorMonto);
    Task NotificarProrrogaAsync(int idCotizacion, DateTime nuevaFechaFin);
    Task NotificarNuevaPreguntaAsync(int idCotizacion, object pregunta);
    Task NotificarNuevaRespuestaAsync(int idCotizacion, object respuesta);
    Task NotificarCierrePorTopeAsync(int idCotizacion);
}
