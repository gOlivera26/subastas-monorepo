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

    public static void CerrarSubasta(int idCotizacion)
    {
        _subastasCerradas.Add(idCotizacion);
    }

    public static void ReabrirSubasta(int idCotizacion)
    {
        _subastasCerradas.Remove(idCotizacion);
    }
}
