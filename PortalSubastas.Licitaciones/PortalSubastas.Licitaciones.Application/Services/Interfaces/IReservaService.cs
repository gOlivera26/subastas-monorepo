using PortalSubastas.Licitaciones.Application.RequestDto.Reserva;
using PortalSubastas.Licitaciones.Application.ResponseDto.Estados;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reserva;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IReservaService
{
    Task<OperationResponse<List<ReservaResponseDto>>> GetAllAsync(ReservaFilterDto? filtros = null);
    Task<OperationResponse<ReservaResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<ReservaResponseDto>> CreateAsync(ReservaRequestDto dto);
    Task<OperationResponse<ReservaResponseDto>> UpdateAsync(int id, ReservaRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
    Task<OperationResponse<ReservaResponseDto>> AutorizarAsync(int id, AutorizarReservaDto dto);
    Task<OperationResponse<ReservaResponseDto>> ClonarAsync(int id);
    Task<OperationResponse<List<EstadoDto>>> GetEstadosAsync();

}