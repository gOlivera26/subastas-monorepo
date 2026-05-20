using PortalSubastas.Providers.Application.RequestDto.Proveedor;
using PortalSubastas.Providers.Application.ResponseDto.Proveedor;

namespace PortalSubastas.Providers.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RubroController : BaseController
{
    private readonly IRubroService _rubroService;

    public RubroController(IRubroService rubroService)
    {
        _rubroService = rubroService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRubros([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        var result = await _rubroService.GetRubrosAsync(page, pageSize, q, sortBy, sortDirection);
        return Return(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRubro([FromBody] CreateRubroDto dto)
    {
        var result = await _rubroService.CreateRubroAsync(dto);
        return Return(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateRubro([FromBody] UpdateRubroDto dto)
    {
        var result = await _rubroService.UpdateRubroAsync(dto);
        return Return(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRubro(int id)
    {
        var result = await _rubroService.DeleteRubroAsync(id);
        return Return(result);
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var result = await _rubroService.GetRubroTreeAsync();
        return Return(result);
    }

    [HttpGet("{parentId}/children")]
    public async Task<IActionResult> GetChildren(int parentId)
    {
        var result = await _rubroService.GetRubroChildrenAsync(parentId);
        return Return(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var result = await _rubroService.SearchRubrosAsync(q);
        return Return(result);
    }
}
