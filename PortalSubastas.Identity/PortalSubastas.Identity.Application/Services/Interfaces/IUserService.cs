namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IUserService
{
    Task<OperationResponse<List<PendingUserDto>>> GetPendingUsersAsync();
    Task<OperationResponse<bool>> ApproveUserAsync(Guid userId);
    Task<OperationResponse<List<ActiveUserDto>>> GetActiveUsersAsync(int page, int pageSize, string searchTerm, string? sortBy = null, string? sortDirection = null);
    Task<OperationResponse<string>> ResetUserPasswordAsync(Guid userId);
    Task<OperationResponse<bool>> UnlinkUserEntityAsync(Guid userId);
    Task<OperationResponse<bool>> UpdateUserRoleAsync(Guid userId, int newRoleId);
    Task<OperationResponse<bool>> LinkUserEntityAsync(Guid userId, LinkEntityRequestDto request);
    Task<OperationResponse<UserAuditDto>> GetUserAuditAsync(Guid userId);
}

