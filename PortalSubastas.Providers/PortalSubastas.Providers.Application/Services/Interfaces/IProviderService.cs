namespace PortalSubastas.Providers.Application.Services.Interfaces;

public interface IProviderService
{
    Task<OperationResponse<ProviderResponseDto>> VerifyCuitAsync(string cuit);
    Task<OperationResponse<ProviderListResponseDto>> GetProvidersAsync(int page, int pageSize, string? searchTerm, string? sortBy = null, string? sortDirection = null);
    Task<OperationResponse<ProviderResponseDto>> CreateProviderAsync(CreateProviderDto dto);
    Task<OperationResponse<ProviderResponseDto>> UpdateProviderAsync(UpdateProviderDto dto);
    Task<OperationResponse<List<ProviderRubroDto>>> GetProviderRubrosAsync(int providerId);
    Task<OperationResponse<bool>> LinkProviderRubrosAsync(int providerId, List<int> rubroIds);
    Task<OperationResponse<bool>> UnlinkProviderRubroAsync(int providerId, int rubroId);
    Task<OperationResponse<string>> UploadConstanciaAfipAsync(int providerId, Stream fileStream, string fileName, string contentType);
}
