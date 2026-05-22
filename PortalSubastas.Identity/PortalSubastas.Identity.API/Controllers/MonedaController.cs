using PortalSubastas.Identity.Application.RequestDto.Moneda;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireModule("clasificadores.monedas")]
public class MonedaController : BaseController
{
    private readonly IMonedaService _s;
    public MonedaController(IMonedaService s) { _s = s; }

    [HttpGet] [Authorize] public async Task<IActionResult> GetAll() => Return(await _s.GetAllAsync());
    [HttpGet("{id:int}")] [Authorize] public async Task<IActionResult> GetById(int id) => Return(await _s.GetByIdAsync(id));
    [HttpPost] [Authorize] public async Task<IActionResult> Create([FromBody] MonedaRequestDto dto) => Return(await _s.CreateAsync(dto));
    [HttpPut("{id:int}")] [Authorize] public async Task<IActionResult> Update(int id, [FromBody] MonedaRequestDto dto) => Return(await _s.UpdateAsync(id, dto));
    [HttpDelete("{id:int}")] [Authorize] public async Task<IActionResult> Delete(int id) => Return(await _s.DeleteAsync(id));
}
