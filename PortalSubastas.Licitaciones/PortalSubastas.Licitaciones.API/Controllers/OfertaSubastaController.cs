using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PortalSubastas.Licitaciones.API.Hubs;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OfertaSubastaController : BaseController
{
    private readonly PortalSubastasContext _context;
    private readonly IHubContext<SubastaHub> _hubContext;

    public OfertaSubastaController(PortalSubastasContext context, IHubContext<SubastaHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> Enviar([FromBody] OfertaRequest request)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == request.IdCotizacion);

        if (cotizacion == null) return NotFound("Cotización no encontrada");
        if (cotizacion.IdEstado != 39) return BadRequest("La subasta no está en curso");

        var ahora = DateTime.Now;
        if (ahora < cotizacion.Especificacion?.FechaInicioSubasta)
            return BadRequest("La subasta aún no comenzó");
        if (ahora > cotizacion.Especificacion?.FechaFinalizacionSubasta)
            return BadRequest("La subasta finalizó");

        var oferta = new TOfertaSubasta
        {
            IdCotizacion = request.IdCotizacion,
            IdProveedor = request.IdProveedor,
            IdCotizacionDetalle = request.IdCotizacionDetalle,
            IdRenglon = request.IdRenglon,
            Monto = request.Monto,
            FechaOferta = ahora,
            UsrIng = User?.Identity?.Name ?? "SISTEMA",
            FecIng = ahora
        };

        _context.TOfertasSubastas.Add(oferta);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group($"subasta_{request.IdCotizacion}")
            .SendAsync("OfertaRecibida", new
            {
                oferta.IdOfertaSubasta,
                oferta.IdCotizacion,
                oferta.IdCotizacionDetalle,
                oferta.IdRenglon,
                oferta.Monto,
                oferta.IdProveedor,
                FechaOferta = oferta.FechaOferta.ToString("yyyy-MM-ddTHH:mm:ss")
            });

        return Ok(new { oferta.IdOfertaSubasta, mensaje = "Oferta registrada" });
    }

    [HttpGet("{idCotizacion:int}")]
    public async Task<IActionResult> Historial(int idCotizacion)
    {
        var ofertas = await _context.TOfertasSubastas
            .Where(o => o.IdCotizacion == idCotizacion)
            .OrderBy(o => o.Monto)
            .ThenBy(o => o.FechaOferta)
            .Select(o => new
            {
                o.IdOfertaSubasta,
                o.IdProveedor,
                o.IdCotizacionDetalle,
                o.IdRenglon,
                o.Monto,
                FechaOferta = o.FechaOferta.ToString("yyyy-MM-ddTHH:mm:ss")
            })
            .ToListAsync();

        return Ok(ofertas);
    }

    public class OfertaRequest
    {
        public int IdCotizacion { get; set; }
        public int IdProveedor { get; set; }
        public int? IdCotizacionDetalle { get; set; }
        public int? IdRenglon { get; set; }
        public decimal Monto { get; set; }
    }
}
