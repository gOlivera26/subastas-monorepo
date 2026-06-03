using PortalSubastas.Identity.Application.RequestDto.Moneda;

[Route("api/[controller]")]
[ApiController]
public class MonedaController : BaseController
{
    private readonly IMonedaService _s;
    public MonedaController(IMonedaService s) { _s = s; }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll() => Return(await _s.GetAllAsync());

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id) => Return(await _s.GetByIdAsync(id));

    [HttpPost]
    [Authorize]
    [RequireModule("clasificadores.monedas")]
    public async Task<IActionResult> Create([FromBody] MonedaRequestDto dto) => Return(await _s.CreateAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize]
    [RequireModule("clasificadores.monedas")]
    public async Task<IActionResult> Update(int id, [FromBody] MonedaRequestDto dto) => Return(await _s.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    [Authorize]
    [RequireModule("clasificadores.monedas")]
    public async Task<IActionResult> Delete(int id) => Return(await _s.DeleteAsync(id));
}