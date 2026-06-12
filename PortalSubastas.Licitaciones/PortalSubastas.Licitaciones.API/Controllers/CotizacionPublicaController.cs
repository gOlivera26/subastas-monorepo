using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion.Publica;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class CotizacionPublicaController : BaseController
{
    private readonly ICotizacionService _cotizacionService;

    public CotizacionPublicaController(ICotizacionService cotizacionService)
    {
        _cotizacionService = cotizacionService;
    }

    /// <summary>
    /// Retorna las subastas públicas activas visibles para el modo espectador.
    /// Filtra solo subastas en curso (IdEstado == 39), pública (Redeterminacion == "1").
    /// </summary>
    [HttpGet("activas")]
    [ProducesResponseType(typeof(OperationResponse<List<SubastaPublicaListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivas()
    {
        var result = await _cotizacionService.GetSubastasPublicasActivasAsync();
        return Return(result);
    }

    /// <summary>
    /// Retorna el detalle público de una subasta activa con sus ítems y mejores ofertas.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OperationResponse<SubastaPublicaDetalleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetalle(int id)
    {
        var result = await _cotizacionService.GetDetalleSubastaPublicaAsync(id);
        return Return(result);
    }
}
