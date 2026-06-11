using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

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
    Task<OperationResponse<CotizacionResponseDto>> ProrrogarAsync(int id, int minutos);
    Task<OperationResponse<CotizacionResponseDto>> DesistirAsync(int id);
    Task<OperationResponse<List<SubastaDashboardDto>>> BuscarAsync(int? idVigencia, int? idEstado, int? idTipoContratacion, string? nro, string? expte, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<OperationResponse<bool>> DesistirParticipacionAsync(int idCotizacion);

    // Endpoints específicos para el Dashboard
    Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasEnCursoAsync(int? idVigencia);
    Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasProximasAsync(int? idVigencia);
    Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasDelMesAsync(int? idVigencia);
    Task<OperationResponse<ReporteLicitacionResponseDto>> GetReporteLicitacionAsync(int idCotizacion);
    Task<OperationResponse<ActaPrelacionReportResponseDto>> GetActaPrelacionAsync(int idCotizacion);
    Task<OperationResponse<DetalleSubastaReportResponseDto>> GetDetalleSubastaAsync(int idCotizacion);
    Task<OperationResponse<ProveedoresInvitadosReportResponseDto>> GetProveedoresInvitadosAsync(int idCotizacion);
    Task<OperationResponse<PreguntasRespuestasReportResponseDto>> GetPreguntasRespuestasAsync(int idCotizacion);
    Task<OperationResponse<DesistimientoReportResponseDto>> GetDesistimientoAsync(int idCotizacion);
    Task<OperationResponse<ObservacionesProveedoresReportResponseDto>> GetObservacionesProveedoresAsync(int idCotizacion);
    Task<OperationResponse<AuditoriaSubastaReportResponseDto>> GetAuditoriaSubastaAsync(int idCotizacion);
    Task<OperationResponse<VerificacionDocumentacionReportResponseDto>> GetVerificacionDocumentacionAsync(int idCotizacion);
}
