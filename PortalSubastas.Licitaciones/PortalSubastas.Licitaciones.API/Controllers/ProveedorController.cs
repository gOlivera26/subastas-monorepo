using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IProveedorService _proveedorService;

    public ProveedorController(PortalSubastasContext context, IProveedorService proveedorService)
    {
        _context = context;
        _proveedorService = proveedorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int idCotizacion)
    {
        var list = await _context.TCotizacionProveedores.Where(p => p.IdCotizacion == idCotizacion && p.FecBaja == null).Select(p => new { p.IdCotizacionProveedor, p.IdProveedor, p.Ganadora }).ToListAsync();
        return Ok(OperationResponse<object>.SuccessResponse(list));
    }

    [HttpPost]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<IActionResult> Add(int idCotizacion, [FromBody] Application.RequestDto.Proveedor.ProveedorAddDto dto)
    {
        var result = await _proveedorService.AddProveedorAsync(idCotizacion, dto);
        return StatusCode(result.Code ?? StatusCodes.Status500InternalServerError, result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<IActionResult> Remove(int idCotizacion, int id)
    {
        var result = await _proveedorService.RemoveProveedorAsync(idCotizacion, id);
        return StatusCode(result.Code ?? StatusCodes.Status500InternalServerError, result);
    }
}