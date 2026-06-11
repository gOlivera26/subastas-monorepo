using PortalSubastas.Reporting.Application.RequestDto;
using PortalSubastas.Reporting.Application.ResponseDto;

namespace PortalSubastas.Reporting.Application.Services.Interfaces;

public interface IReportService
{
    IReadOnlyCollection<ReporteCatalogoResponseDto> GetCatalogo();

    Task<string> GenerarLicitacionHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReporteDocumentoResponseDto> GenerarLicitacionPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> GenerarDetalleSubastaHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReporteDocumentoResponseDto> GenerarDetalleSubastaPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> GenerarProveedoresInvitadosHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReporteDocumentoResponseDto> GenerarProveedoresInvitadosPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> GenerarPreguntasRespuestasHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReporteDocumentoResponseDto> GenerarPreguntasRespuestasPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> GenerarDesistimientoHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReporteDocumentoResponseDto> GenerarDesistimientoPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> GenerarObservacionesProveedoresHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReporteDocumentoResponseDto> GenerarObservacionesProveedoresPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> GenerarAuditoriaSubastaHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReporteDocumentoResponseDto> GenerarAuditoriaSubastaPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<string> GenerarVerificacionDocumentacionHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ReporteDocumentoResponseDto> GenerarVerificacionDocumentacionPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default);
}
