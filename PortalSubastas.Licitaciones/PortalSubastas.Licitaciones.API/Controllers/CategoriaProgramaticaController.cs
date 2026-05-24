using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Catalogos;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriaProgramaticaController : ControllerBase
{
    private readonly ICategoriaProgramaticaService _service;

    public CategoriaProgramaticaController(ICategoriaProgramaticaService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<CategoriaProgramaticaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int? vigencia = null, [FromQuery] int? unidadAdm = null)
    {
        var filtros = new CategoriaProgramaticaFilterDto
        {
            Vigencia = vigencia,
            UnidadAdm = unidadAdm
        };

        var result = await _service.GetByFilterAsync(filtros);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }
}
