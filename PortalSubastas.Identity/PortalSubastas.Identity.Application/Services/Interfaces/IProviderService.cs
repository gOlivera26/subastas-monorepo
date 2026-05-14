using PortalSubastas.Identity.Application.ResponseDto.Proveedor;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IProviderService
{
    Task<OperationResponse<ProviderResponseDto>> VerifyCuitAsync(string cuit);
}
