using PortalSubastas.Identity.Application.ResponseDto.Organizacion;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IOrganizationService
{
    Task<OperationResponse<List<OrganizationResponseDto>>> GetAllActiveAsync();
}
