using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Ganador;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Ganador;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GanadorController : BaseController
{
    private readonly IGanadorService _ganadorService;

    public GanadorController(IGanadorService ganadorService)
    {
        _ganadorService = ganadorService;
    }

    [HttpGet("{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<List<GanadorResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int idCotizacion)
    {
        var result = await _ganadorService.GetAllAsync(idCotizacion);
        return Return(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse<GanadorResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] GanadorRequestDto dto)
    {
        var result = await _ganadorService.CreateAsync(dto);
        return Return(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _ganadorService.DeleteAsync(id);
        return Return(result);
    }
}
