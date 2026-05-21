using PortalSubastas.Identity.Application.RequestDto.Organizacion;
using PortalSubastas.Identity.Application.ResponseDto.Organizacion;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IOrganizationService
{
    Task<OperationResponse<List<OrganizationResponseDto>>> GetAllAsync();
    Task<OperationResponse<List<OrganizationResponseDto>>> GetAllActiveAsync();
    Task<OperationResponse<OrganizationResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<OrganizationResponseDto>> CreateAsync(OrganizationRequestDto dto);
    Task<OperationResponse<OrganizationResponseDto>> UpdateAsync(int id, OrganizationRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
}
