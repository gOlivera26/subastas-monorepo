using PortalSubastas.Identity.Application.RequestDto.UnidadAdministrativa;
using PortalSubastas.Identity.Application.ResponseDto.UnidadAdministrativa;
using PortalSubastas.Identity.API.Middlewares;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireModule("clasificadores.unidades-administrativas")]
public class UnidadAdministrativaController : BaseController
{
    private readonly IUnidadAdministrativaService _unidadService;

    public UnidadAdministrativaController(IUnidadAdministrativaService unidadService)
    {
        _unidadService = unidadService;
    }

    [HttpGet("vigencia/{idVigencia:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<List<UnidadAdministrativaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVigencia(int idVigencia)
    {
        var result = await _unidadService.GetByVigenciaAsync(idVigencia);
        return Return(result);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<UnidadAdministrativaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _unidadService.GetByIdAsync(id);
        return Return(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<UnidadAdministrativaResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] UnidadAdministrativaRequestDto dto)
    {
        var result = await _unidadService.CreateAsync(dto);
        return Return(result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<UnidadAdministrativaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UnidadAdministrativaRequestDto dto)
    {
        var result = await _unidadService.UpdateAsync(id, dto);
        return Return(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _unidadService.DeleteAsync(id);
        return Return(result);
    }
}
