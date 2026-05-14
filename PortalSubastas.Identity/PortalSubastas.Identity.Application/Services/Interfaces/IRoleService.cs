using PortalSubastas.Identity.Application.ResponseDto.Role;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IRoleService
{
    Task<OperationResponse<List<RoleResponseDto>>> GetActiveRolesAsync();
}
