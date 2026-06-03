using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PortalSubastas.Licitaciones.API.Hubs;

[Authorize]
public class SubastaHub : Hub
{

    private readonly PortalSubastasContext _context;

    public SubastaHub(PortalSubastasContext context)
    {
        _context = context;
    }

    // --- Gestión de Salas de Subastas ---
    public async Task UnirseSubasta(int idCotizacion)
    {
        await ValidarAccesoPrivadoAsync(idCotizacion);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"subasta_{idCotizacion}");
    }

    public async Task SalirSubasta(int idCotizacion)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"subasta_{idCotizacion}");
    }

    // --- Mensajería en tiempo real (Chat/Aclaraciones) ---
    public async Task UnirseMensajes(int idCotizacion)
    {
        await ValidarAccesoPrivadoAsync(idCotizacion);
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

    private async Task ValidarAccesoPrivadoAsync(int idCotizacion)
    {
        var isSuperAdmin = Context.User?.IsInRole("SUPERADMIN") ?? false;
        if (isSuperAdmin) return; // Los admins pueden entrar a cualquier sala

        var strIdProveedor = Context.User?.FindFirst("IdProveedor")?.Value;
        int.TryParse(strIdProveedor, out int idProveedor);

        var especificacion = await _context.TCotizacionEspecificaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdCotizacion == idCotizacion);

        // Si existe y no es Pública (1 = Pública, 0 = Privada, 2 = Cerrada)
        if (especificacion != null && especificacion.Redeterminacion != "1")
        {
            bool estaInvitado = await _context.TCotizacionProveedores
                .AnyAsync(p => p.IdCotizacion == idCotizacion && p.IdProveedor == idProveedor && p.FecBaja == null);

            if (!estaInvitado)
            {
                throw new HubException("Acceso denegado: La subasta es de carácter privado/cerrado y no posee invitación.");
            }
        }
    }
}