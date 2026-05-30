using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Garantia;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Garantia;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GarantiaController : BaseController
{
    private readonly IGarantiaService _garantiaService;

    public GarantiaController(IGarantiaService garantiaService)
    {
        _garantiaService = garantiaService;
    }

    [HttpGet("{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<List<GarantiaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int idCotizacion)
    {
        var provIdStr = HttpContext.User?.FindFirst("IdProveedor")?.Value;
        int.TryParse(provIdStr, out int provId);

        var result = await _garantiaService.GetByCotizacionAsync(idCotizacion, provId > 0 ? provId : null);
        return Return(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse<GarantiaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromForm] GarantiaRequestDto request)
    {
        var result = await _garantiaService.CreateAsync(request);
        return Return(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _garantiaService.DeleteAsync(id);
        return Return(result);
    }
}