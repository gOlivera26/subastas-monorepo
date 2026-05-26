using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PortalSubastas.Licitaciones.API.Hubs;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Cotizacion/{idCotizacion:int}/[controller]")]
public class MensajeController : ControllerBase
{
    private readonly PortalSubastasContext _context;
    private readonly IHttpContextAccessor _http;
    private readonly IHubContext<SubastaHub> _hub;

    public MensajeController(PortalSubastasContext context, IHttpContextAccessor http, IHubContext<SubastaHub> hub)
    {
        _context = context;
        _http = http;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int idCotizacion)
    {
        var mensajes = await _context.TMensajes
            .Where(m => m.IdCotizacion == idCotizacion)
            .OrderByDescending(m => m.FecIng)
            .Take(100)
            .Select(m => new { m.IdMensaje, m.Usuario, m.Contenido, m.FecIng, m.IdProveedor })
            .ToListAsync();
        return Ok(new { success = true, data = mensajes.OrderBy(m => m.FecIng) });
    }

    [HttpPost]
    public async Task<IActionResult> Send(int idCotizacion, [FromBody] MensajeDto dto)
    {
        var usuario = _http.HttpContext?.User?.Identity?.Name ?? "Anónimo";
        var entity = new TMensaje
        {
            IdCotizacion = idCotizacion,
            IdProveedor = dto.IdProveedor,
            Usuario = usuario,
            Contenido = dto.Contenido,
            FecIng = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };
        _context.TMensajes.Add(entity);
        await _context.SaveChangesAsync();

        var msg = new { entity.IdMensaje, entity.Usuario, entity.Contenido, entity.FecIng };
        await _hub.Clients.Group($"chat_{idCotizacion}").SendAsync("MensajeRecibido", msg);

        return Ok(new { success = true, data = msg });
    }
}

public class MensajeDto
{
    public string Contenido { get; set; }
    public int? IdProveedor { get; set; }
}
