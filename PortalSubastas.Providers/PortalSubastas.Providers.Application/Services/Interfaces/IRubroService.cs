namespace PortalSubastas.Providers.Application.Services.Interfaces;

public interface IRubroService
{
    Task<OperationResponse<RubroListResponseDto>> GetRubrosAsync(int page, int pageSize, string? searchTerm, string? sortBy = null, string? sortDirection = null);
    Task<OperationResponse<RubroListDto>> CreateRubroAsync(CreateRubroDto dto);
    Task<OperationResponse<RubroListDto>> UpdateRubroAsync(UpdateRubroDto dto);
    Task<OperationResponse<bool>> DeleteRubroAsync(int id);
    Task<OperationResponse<List<RubroTreeDto>>> GetRubroTreeAsync();
    Task<OperationResponse<List<RubroTreeDto>>> GetRubroChildrenAsync(int parentId);
    Task<OperationResponse<List<RubroSearchResultDto>>> SearchRubrosAsync(string query);
}
