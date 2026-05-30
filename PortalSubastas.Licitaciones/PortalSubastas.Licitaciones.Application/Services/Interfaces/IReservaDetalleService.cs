using PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.ResponseDto.ReservaDetalle;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IReservaDetalleService
{
    Task<OperationResponse<List<ReservaDetalleResponseDto>>> GetByReservaIdAsync(int reservaId);
    Task<OperationResponse<ReservaDetalleResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<ReservaDetalleResponseDto>> CreateAsync(ReservaDetalleRequestDto dto);
    Task<OperationResponse<ReservaDetalleResponseDto>> UpdateAsync(int id, ReservaDetalleRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
    Task<OperationResponse<bool>> DesautorizarAsync(int id);
}
