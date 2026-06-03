using Microsoft.AspNetCore.SignalR;
using PortalSubastas.Licitaciones.API.Hubs;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Services;

public class SubastaNotificationService : ISubastaNotificationService
{
    private readonly IHubContext<SubastaHub> _hubContext;

    public SubastaNotificationService(IHubContext<SubastaHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotificarNuevaOfertaAsync(int idCotizacion, int idOfertaSubasta, int? idCotizacionDetalle, int? idRenglon, decimal monto, int idProveedor, DateTime fechaOferta)
    {
        var payloadOferta = new
        {
            idCotizacion = idCotizacion,
            idCotizacionDetalle = idCotizacionDetalle,
            idRenglon = idRenglon,
            monto = (double)monto,
            idProveedor = idProveedor,
            fecha = fechaOferta.ToString("yyyy-MM-ddTHH:mm:ss"),
            usuario = $"Proveedor #{idProveedor}"
        };

        await _hubContext.Clients.Group($"subasta_{idCotizacion}").SendAsync("OfertaRecibida", payloadOferta);
    }

    public async Task NotificarProrrogaAsync(int idCotizacion, DateTime nuevaFechaFin)
    {
        await _hubContext.Clients.Group($"subasta_{idCotizacion}").SendAsync("ProrrogaAplicada", new
        {
            idCotizacion = idCotizacion,
            nuevaFechaFin = nuevaFechaFin.ToString("yyyy-MM-ddTHH:mm:ss")
        });
    }

    public async Task NotificarNuevaPreguntaAsync(int idCotizacion, object pregunta)
    {
        await _hubContext.Clients.Group($"chat_{idCotizacion}").SendAsync("PreguntaRecibida", pregunta);
    }

    public async Task NotificarNuevaRespuestaAsync(int idCotizacion, object respuesta)
    {
        await _hubContext.Clients.Group($"chat_{idCotizacion}").SendAsync("RespuestaRecibida", respuesta);
    }

    public async Task NotificarCierrePorTopeAsync(int idCotizacion)
    {
        await _hubContext.Clients.Group($"subasta_{idCotizacion}").SendAsync("SubastaCerradaPorTope", idCotizacion);
    }
}