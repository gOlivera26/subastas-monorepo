using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Cotizacion/{idCotizacion:int}/[controller]")]
public class ProveedorController : ControllerBase
{
    private readonly PortalSubastasContext _context;
    private readonly IHttpContextAccessor _http;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProveedorController> _logger;
    private readonly IProveedorService _proveedorService;

    public ProveedorController(
        PortalSubastasContext context,
        IHttpContextAccessor http,
        IPublishEndpoint publishEndpoint,
        ILogger<ProveedorController> logger,
        IProveedorService proveedorService)
    {
        _context = context;
        _http = http;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _proveedorService = proveedorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int idCotizacion)
    {
        var list = await _context.TCotizacionProveedores
            .Where(p => p.IdCotizacion == idCotizacion && p.FecBaja == null)
            .Select(p => new { p.IdCotizacionProveedor, p.IdProveedor, p.Ganadora })
            .ToListAsync();
        return Ok(OperationResponse<object>.SuccessResponse(list));
    }

    [HttpPost]
    public async Task<IActionResult> Add(int idCotizacion, [FromBody] Application.RequestDto.Proveedor.ProveedorAddDto dto)
    {
        var result = await _proveedorService.AddProveedorAsync(idCotizacion, dto);
        if (result.Success == false)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int idCotizacion, int id)
    {
        var entity = await _context.TCotizacionProveedores.FindAsync(id);
        if (entity == null || entity.IdCotizacion != idCotizacion)
            return NotFound();
        entity.FecBaja = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        entity.UsrBaja = _http.HttpContext?.User?.Identity?.Name ?? "SISTEMA";
        await _context.SaveChangesAsync();
        return Ok(OperationResponse<bool>.SuccessResponse(true));
    }
}
