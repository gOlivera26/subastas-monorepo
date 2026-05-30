using PortalSubastas.Licitaciones.Application.RequestDto.Garantia;
using PortalSubastas.Licitaciones.Application.ResponseDto.Garantia;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IGarantiaService
{
    Task<OperationResponse<List<GarantiaResponseDto>>> GetByCotizacionAsync(int idCotizacion, int? idProveedor);
    Task<OperationResponse<GarantiaResponseDto>> CreateAsync(GarantiaRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
}
