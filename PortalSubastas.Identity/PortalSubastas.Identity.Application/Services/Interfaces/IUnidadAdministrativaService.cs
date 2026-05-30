using PortalSubastas.Identity.Application.RequestDto.UnidadAdministrativa;
using PortalSubastas.Identity.Application.ResponseDto.UnidadAdministrativa;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IUnidadAdministrativaService
{
    Task<OperationResponse<List<UnidadAdministrativaResponseDto>>> GetByVigenciaAsync(int idVigencia);
    Task<OperationResponse<List<UnidadAdministrativaResponseDto>>> GetAllAsync();
    Task<OperationResponse<UnidadAdministrativaResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<UnidadAdministrativaResponseDto>> CreateAsync(UnidadAdministrativaRequestDto dto);
    Task<OperationResponse<UnidadAdministrativaResponseDto>> UpdateAsync(int id, UnidadAdministrativaRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
}
