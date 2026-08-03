using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Tablero;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class TableroController : BaseController
{
    private readonly ITableroService _tableroService;

    public TableroController(ITableroService tableroService)
    {
        _tableroService = tableroService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<TableroResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] int criterio = 0,
        [FromQuery] int? idVigencia = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _tableroService.GetAsync(criterio, idVigencia, cancellationToken);
        return Return(result);
    }
}
