using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface ICotizacionService
{
    Task<OperationResponse<List<CotizacionResponseDto>>> GetAllAsync(int? idVigencia);
    Task<OperationResponse<CotizacionResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<CotizacionResponseDto>> CreateAsync(CotizacionRequestDto dto);
    Task<OperationResponse<CotizacionResponseDto>> UpdateAsync(int id, CotizacionRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
    Task<OperationResponse<CotizacionResponseDto>> NotificarAsync(int id);
    Task<OperationResponse<CotizacionResponseDto>> FinalizarAsync(int id);

    // Endpoints específicos para el Dashboard
    Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasEnCursoAsync(int? idVigencia);
    Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasProximasAsync(int? idVigencia);
    Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasDelMesAsync(int? idVigencia);
}
