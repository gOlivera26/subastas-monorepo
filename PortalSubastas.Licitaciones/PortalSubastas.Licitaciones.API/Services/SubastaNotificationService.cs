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

    public async Task NotificarNuevaOfertaAsync(int idCotizacion, int idOfertaSubasta, int? idCotizacionDetalle, int? idRenglon, decimal monto, int idProveedor, DateTime fechaOferta, string? proveedor = null, string? representante = null)
    {
        var payloadOferta = new
        {
            idCotizacion = idCotizacion,
            idCotizacionDetalle = idCotizacionDetalle,
            idRenglon = idRenglon,
            monto = (double)monto,
            idProveedor = idProveedor,
            fecha = fechaOferta.ToString("yyyy-MM-ddTHH:mm:ss"),
            proveedor = proveedor ?? $"Proveedor #{idProveedor}",
            representante = representante,
            usuario = string.IsNullOrWhiteSpace(representante)
                ? (proveedor ?? $"Proveedor #{idProveedor}")
                : representante
        };

        await _hubContext.Clients.Group($"subasta_{idCotizacion}_proveedor_{idProveedor}").SendAsync("OfertaRecibida", payloadOferta);
    }

    public async Task NotificarMejorOfertaActualizadaAsync(int idCotizacion, int? idCotizacionDetalle, int? idRenglon, decimal mejorMonto)
    {
        await _hubContext.Clients.Group($"subasta_{idCotizacion}").SendAsync("MejorOfertaActualizada", new
        {
            idCotizacion = idCotizacion,
            idCotizacionDetalle = idCotizacionDetalle,
            idRenglon = idRenglon,
            mejorMonto = (double)mejorMonto
        });
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
