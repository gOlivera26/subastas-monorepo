using PortalSubastas.Identity.Application.RequestDto.Moneda;
using PortalSubastas.Identity.Application.ResponseDto.Moneda;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IMonedaService
{
    Task<OperationResponse<List<MonedaResponseDto>>> GetAllAsync();
    Task<OperationResponse<MonedaResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<MonedaResponseDto>> CreateAsync(MonedaRequestDto dto);
    Task<OperationResponse<MonedaResponseDto>> UpdateAsync(int id, MonedaRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
}
