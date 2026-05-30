using PortalSubastas.Licitaciones.Application.RequestDto.Ganador;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Ganador;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IGanadorService
{
    Task<OperationResponse<List<GanadorResponseDto>>> GetAllAsync(int idCotizacion);
    Task<OperationResponse<GanadorResponseDto>> CreateAsync(GanadorRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
}
