using PortalSubastas.Identity.Application.RequestDto.Vigencia;
using PortalSubastas.Identity.Application.ResponseDto.Vigencia;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VigenciaController : BaseController
{
    private readonly IVigenciaService _vigenciaService;

    public VigenciaController(IVigenciaService vigenciaService)
    {
        _vigenciaService = vigenciaService;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<List<VigenciaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _vigenciaService.GetAllAsync();
        return Return(result);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<VigenciaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _vigenciaService.GetByIdAsync(id);
        return Return(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<VigenciaResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] VigenciaRequestDto dto)
    {
        var result = await _vigenciaService.CreateAsync(dto);
        return Return(result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<VigenciaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] VigenciaRequestDto dto)
    {
        var result = await _vigenciaService.UpdateAsync(id, dto);
        return Return(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _vigenciaService.DeleteAsync(id);
        return Return(result);
    }

    [HttpPost("{id:int}/activar-ejecucion")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<VigenciaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivarEjecucion(int id)
    {
        var result = await _vigenciaService.SetActivaEjecucionAsync(id);
        return Return(result);
    }
}
