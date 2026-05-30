using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MonedaController : ControllerBase
{
    private readonly IMonedaService _service;

    public MonedaController(IMonedaService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<MonedaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var result = await _service.GetAllAsync();
        return result.Success == true ? Ok(result) : BadRequest(result);
    }
}
