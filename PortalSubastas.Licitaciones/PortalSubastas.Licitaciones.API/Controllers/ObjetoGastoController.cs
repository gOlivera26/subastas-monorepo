using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ObjetoGastoController : ControllerBase
{
    private readonly IObjetoGastoService _service;

    public ObjetoGastoController(IObjetoGastoService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<ObjetoGastoResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] bool? vigente = null)
    {
        var result = await _service.GetByFilterAsync(vigente);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }
}
