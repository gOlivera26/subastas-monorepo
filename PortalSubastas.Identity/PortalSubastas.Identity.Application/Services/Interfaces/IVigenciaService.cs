using PortalSubastas.Identity.Application.RequestDto.Vigencia;
using PortalSubastas.Identity.Application.ResponseDto.Vigencia;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IVigenciaService
{
    Task<OperationResponse<List<VigenciaResponseDto>>> GetAllAsync();
    Task<OperationResponse<VigenciaResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<VigenciaResponseDto>> CreateAsync(VigenciaRequestDto dto);
    Task<OperationResponse<VigenciaResponseDto>> UpdateAsync(int id, VigenciaRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
    Task<OperationResponse<VigenciaResponseDto>> SetActivaEjecucionAsync(int id);
}
