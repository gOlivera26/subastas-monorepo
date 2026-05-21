using PortalSubastas.Identity.Application.RequestDto.ObjetoGasto;
using PortalSubastas.Identity.Application.ResponseDto.ObjetoGasto;
using PortalSubastas.Identity.API.Middlewares;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireModule("clasificadores.objetos-gasto")]
public class ObjetoGastoController : BaseController
{
    private readonly IObjetoGastoService _service;

    public ObjetoGastoController(IObjetoGastoService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] int? idVigencia)
    {
        var result = await _service.GetAllAsync(idVigencia);
        return Return(result);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Return(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ObjetoGastoRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Return(result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] ObjetoGastoRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Return(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return Return(result);
    }
}
