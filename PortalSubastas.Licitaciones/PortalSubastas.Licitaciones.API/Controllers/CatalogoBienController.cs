using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Catalogos;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CatalogoBienController : ControllerBase
{
    private readonly ICatalogoBienService _service;

    public CatalogoBienController(ICatalogoBienService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<CatalogoBienResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] bool? vigente = null, [FromQuery] int? jurisdiccion = null, [FromQuery] int? categoria = null)
    {
        var filtros = new CatalogoBienFilterDto
        {
            Vigente = vigente,
            Jurisdiccion = jurisdiccion,
            CategoriaBien = categoria
        };

        var result = await _service.GetByFilterAsync(filtros);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }
}
