using PortalSubastas.Identity.Application.RequestDto.Role;
using PortalSubastas.Identity.Application.ResponseDto;
using PortalSubastas.Identity.Application.ResponseDto.Role;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleController : BaseController
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<List<RoleResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _roleService.GetAllAsync();
        return Return(result);
    }

    [HttpGet("active")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<List<RoleResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveRoles()
    {
        var result = await _roleService.GetActiveRolesAsync();
        return Return(result);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _roleService.GetByIdAsync(id);
        return Return(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] RoleRequestDto dto)
    {
        var result = await _roleService.CreateAsync(dto);
        return Return(result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] RoleRequestDto dto)
    {
        var result = await _roleService.UpdateAsync(id, dto);
        return Return(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _roleService.DeleteAsync(id);
        return Return(result);
    }

    [HttpGet("{idRol:int}/modulos")]
    [Authorize]
    public async Task<IActionResult> GetModules(int idRol)
    {
        var result = await _roleService.GetModulesByRoleAsync(idRol);
        return Return(result);
    }

    [HttpPost("modulos")]
    [Authorize]
    public async Task<IActionResult> AssignModule([FromBody] RoleModuleRequestDto dto)
    {
        var result = await _roleService.AssignModuleAsync(dto);
        return Return(result);
    }

    [HttpDelete("modulos/{idRol:int}/{idModulo:int}")]
    [Authorize]
    public async Task<IActionResult> UnassignModule(int idRol, int idModulo)
    {
        var result = await _roleService.UnassignModuleAsync(idRol, idModulo);
        return Return(result);
    }

    [HttpGet("modulos")]
    [Authorize]
    public async Task<IActionResult> GetAllModules()
    {
        var result = await _roleService.GetAllModulesAsync();
        return Return(result);
    }

    [HttpGet("paginas")]
    [Authorize]
    public async Task<IActionResult> GetAllPages()
    {
        var result = await _roleService.GetAllPagesAsync();
        return Return(result);
    }

    [HttpGet("{idRol:int}/paginas")]
    [Authorize]
    public async Task<IActionResult> GetPagesByRole(int idRol)
    {
        var result = await _roleService.GetPagesByRoleAsync(idRol);
        return Return(result);
    }

    [HttpPost("paginas")]
    [Authorize]
    public async Task<IActionResult> AssignPage([FromBody] RoleModuleRequestDto dto)
    {
        var result = await _roleService.AssignPageAsync(dto.IdRol, dto.IdModulo);
        return Return(result);
    }

    [HttpDelete("paginas/{idRol:int}/{idPagina:int}")]
    [Authorize]
    public async Task<IActionResult> UnassignPage(int idRol, int idPagina)
    {
        var result = await _roleService.UnassignPageAsync(idRol, idPagina);
        return Return(result);
    }

    [HttpGet("modulos-con-paginas")]
    [Authorize]
    public async Task<IActionResult> GetModulosConPaginas()
    {
        var result = await _roleService.GetModulosConPaginasAsync();
        return Return(result);
    }
}
