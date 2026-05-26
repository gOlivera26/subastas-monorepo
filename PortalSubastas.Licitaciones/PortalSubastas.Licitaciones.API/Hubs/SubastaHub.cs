using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.API.Hubs;

[Authorize]
public class SubastaHub : Hub
{
    private static readonly HashSet<int> _subastasCerradas = new();

    public async Task UnirseSubasta(int idCotizacion)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"subasta_{idCotizacion}");
    }

    public async Task SalirSubasta(int idCotizacion)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"subasta_{idCotizacion}");
    }

    public async Task<bool> EnviarOferta(int idCotizacion, int? idCotizacionDetalle, int? idRenglon, decimal monto, int idProveedor)
    {
        if (_subastasCerradas.Contains(idCotizacion))
            return false;

        var oferta = new
        {
            idCotizacion,
            idCotizacionDetalle,
            idRenglon,
            monto,
            idProveedor,
            fecha = DateTime.Now,
            connectionId = Context.ConnectionId,
            usuario = Context.User?.Identity?.Name ?? "Anónimo"
        };

        await Clients.Group($"subasta_{idCotizacion}").SendAsync("OfertaRecibida", oferta);
        return true;
    }

    // --- Mensajería en tiempo real ---
    public async Task UnirseMensajes(int idCotizacion)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{idCotizacion}");
    }

    public async Task SalirMensajes(int idCotizacion)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{idCotizacion}");
    }

    public async Task EnviarMensaje(int idCotizacion, string contenido)
    {
        var usuario = Context.User?.Identity?.Name ?? "Anónimo";
        var mensaje = new
        {
            usuario,
            contenido,
            fecIng = DateTime.Now
        };

        await Clients.Group($"chat_{idCotizacion}").SendAsync("MensajeRecibido", mensaje);
    }

    public async Task Escribiendo(int idCotizacion)
    {
        var usuario = Context.User?.Identity?.Name ?? "Anónimo";
        await Clients.OthersInGroup($"chat_{idCotizacion}").SendAsync("UsuarioEscribiendo", usuario);
    }

    public static void CerrarSubasta(int idCotizacion)
    {
        _subastasCerradas.Add(idCotizacion);
    }

    public static void ReabrirSubasta(int idCotizacion)
    {
        _subastasCerradas.Remove(idCotizacion);
    }
}
