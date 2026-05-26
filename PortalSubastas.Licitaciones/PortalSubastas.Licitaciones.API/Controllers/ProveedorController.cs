using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Cotizacion/{idCotizacion:int}/[controller]")]
public class ProveedorController : ControllerBase
{
    private readonly PortalSubastasContext _context;
    private readonly IHttpContextAccessor _http;

    public ProveedorController(PortalSubastasContext context, IHttpContextAccessor http)
    {
        _context = context;
        _http = http;
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
    public async Task<IActionResult> Add(int idCotizacion, [FromBody] ProveedorAddDto dto)
    {
        if (await _context.TCotizacionProveedores.AnyAsync(p => p.IdCotizacion == idCotizacion && p.IdProveedor == dto.IdProveedor && p.FecBaja == null))
            return BadRequest(OperationResponse<object>.CustomErrorResponse(400, "El proveedor ya está asignado."));

        var entity = new TCotizacionProveedor
        {
            IdCotizacion = idCotizacion,
            IdProveedor = dto.IdProveedor,
            Ganadora = dto.Ganadora ?? "N",
            UsrIng = _http.HttpContext?.User?.Identity?.Name ?? "SISTEMA",
            FecIng = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };
        _context.TCotizacionProveedores.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(OperationResponse<object>.SuccessResponse(new { entity.IdCotizacionProveedor }));
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

public class ProveedorAddDto
{
    public int IdProveedor { get; set; }
    public string? Ganadora { get; set; }
}
