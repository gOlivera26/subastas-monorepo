using PortalSubastas.Identity.Application.RequestDto.CatalogoBien;
using PortalSubastas.Identity.Application.ResponseDto.CatalogoBien;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface ICatalogoBienService
{
    Task<OperationResponse<List<CatalogoBienResponseDto>>> GetAllAsync(int? idVigencia);
    Task<OperationResponse<CatalogoBienResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<CatalogoBienResponseDto>> CreateAsync(CatalogoBienRequestDto dto);
    Task<OperationResponse<CatalogoBienResponseDto>> UpdateAsync(int id, CatalogoBienRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
}
