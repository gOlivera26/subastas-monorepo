using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.RequestDto.OfertaSubasta;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.OfertaSubasta;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OfertaSubastaController : BaseController
{
    private readonly IOfertaSubastaService _ofertaService;

    public OfertaSubastaController(IOfertaSubastaService ofertaService)
    {
        _ofertaService = ofertaService;
    }

    [HttpPost("{idCotizacion:int}/Batch")]
    [ProducesResponseType(typeof(OperationResponse<List<OfertaItemResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnviarBatch(int idCotizacion, [FromBody] List<OfertaItemRequestDto> ofertas)
    {
        var result = await _ofertaService.ProcesarOfertasAsync(idCotizacion, ofertas);
        return Return(result);
    }

    [HttpGet("{idCotizacion:int}")]
    public async Task<IActionResult> Historial(int idCotizacion)
    {
        var result = await _ofertaService.GetHistorialAsync(idCotizacion);
        return Return(result);
    }
}