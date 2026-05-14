using PortalSubastas.Identity.Application.RequestDto.Users;
using PortalSubastas.Identity.Application.ResponseDto.Users;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IUserService
{
    Task<OperationResponse<List<PendingUserDto>>> GetPendingUsersAsync();
    Task<OperationResponse<bool>> ApproveUserAsync(Guid userId);
    Task<OperationResponse<List<ActiveUserDto>>> GetActiveUsersAsync(int page, int pageSize, string searchTerm);
    Task<OperationResponse<string>> ResetUserPasswordAsync(Guid userId);
    Task<OperationResponse<bool>> UnlinkUserEntityAsync(Guid userId);
    Task<OperationResponse<bool>> UpdateUserRoleAsync(Guid userId, int newRoleId);
    Task<OperationResponse<bool>> LinkUserEntityAsync(Guid userId, LinkEntityRequestDto request);
    Task<OperationResponse<UserAuditDto>> GetUserAuditAsync(Guid userId);
}

