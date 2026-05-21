using PortalSubastas.Identity.Application.RequestDto.CategoriaProgramatica;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireModule("clasificadores.categorias-programaticas")]
public class CategoriaProgramaticaController : BaseController
{
    private readonly ICategoriaProgramaticaService _s;
    public CategoriaProgramaticaController(ICategoriaProgramaticaService s) { _s = s; }

    [HttpGet] [Authorize] public async Task<IActionResult> GetAll([FromQuery] int? idVigencia) => Return(await _s.GetAllAsync(idVigencia));
    [HttpGet("{id:int}")] [Authorize] public async Task<IActionResult> GetById(int id) => Return(await _s.GetByIdAsync(id));
    [HttpPost] [Authorize] public async Task<IActionResult> Create([FromBody] CategoriaProgramaticaRequestDto dto) => Return(await _s.CreateAsync(dto));
    [HttpPut("{id:int}")] [Authorize] public async Task<IActionResult> Update(int id, [FromBody] CategoriaProgramaticaRequestDto dto) => Return(await _s.UpdateAsync(id, dto));
    [HttpDelete("{id:int}")] [Authorize] public async Task<IActionResult> Delete(int id) => Return(await _s.DeleteAsync(id));
}
