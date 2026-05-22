using PortalSubastas.Identity.Application.RequestDto.SubResponsable;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[RequireModule("clasificadores.areas")]
public class SubResponsableController : BaseController
{
    private readonly ISubResponsableService _s;
    public SubResponsableController(ISubResponsableService s) { _s = s; }

    [HttpGet] [Authorize] public async Task<IActionResult> GetAll([FromQuery] int? idUnidadAdm) => Return(await _s.GetAllAsync(idUnidadAdm));
    [HttpGet("{id:int}")] [Authorize] public async Task<IActionResult> GetById(int id) => Return(await _s.GetByIdAsync(id));
    [HttpPost] [Authorize] public async Task<IActionResult> Create([FromBody] SubResponsableRequestDto dto) => Return(await _s.CreateAsync(dto));
    [HttpPut("{id:int}")] [Authorize] public async Task<IActionResult> Update(int id, [FromBody] SubResponsableRequestDto dto) => Return(await _s.UpdateAsync(id, dto));
    [HttpDelete("{id:int}")] [Authorize] public async Task<IActionResult> Delete(int id) => Return(await _s.DeleteAsync(id));

    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> Upload([FromBody] SubResponsableBulkUploadDto bulk)
    {
        return Return(await _s.UploadCsvAsync(bulk));
    }
}
