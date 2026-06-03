using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface ICotizacionDocumentoService
{
    Task<OperationResponse<List<CotizacionDocumentoResponseDto>>> GetByCotizacionAsync(int idCotizacion);
    Task<OperationResponse<CotizacionDocumentoResponseDto>> CreateAsync(CotizacionDocumentoRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
}
