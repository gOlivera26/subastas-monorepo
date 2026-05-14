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

    /// <summary>
    /// Obtiene la lista completa de todos los roles activos disponibles en el sistema.
    /// </summary>
    /// <returns>Una respuesta HTTP que contiene una lista de objetos <see cref="RoleResponseDto"/> con el ID, Nombre y Descripción de cada rol.</returns>
    [HttpGet("active")]
    [Authorize]
    [ProducesResponseType(typeof(OperationResponse<List<RoleResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveRoles()
    {
        var result = await _roleService.GetActiveRolesAsync();
        return Return(result);
    }
}