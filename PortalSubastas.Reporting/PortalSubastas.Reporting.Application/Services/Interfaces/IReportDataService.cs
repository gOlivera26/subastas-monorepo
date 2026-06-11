namespace PortalSubastas.Reporting.Application.Services.Interfaces;

public interface IReportDataService
{
    Task<ReporteLicitacion> GetLicitacionAsync(int idCotizacion, CancellationToken cancellationToken = default);
    Task<ActaPrelacionReport> GetActaPrelacionAsync(int idCotizacion, CancellationToken cancellationToken = default);
    Task<DetalleSubastaReport> GetDetalleSubastaAsync(int idCotizacion, CancellationToken cancellationToken = default);
    Task<ProveedoresInvitadosReport> GetProveedoresInvitadosAsync(int idCotizacion, CancellationToken cancellationToken = default);
    Task<PreguntasRespuestasReport> GetPreguntasRespuestasAsync(int idCotizacion, CancellationToken cancellationToken = default);
    Task<DesistimientoReport> GetDesistimientoAsync(int idCotizacion, CancellationToken cancellationToken = default);
    Task<ObservacionesProveedoresReport> GetObservacionesProveedoresAsync(int idCotizacion, CancellationToken cancellationToken = default);
    Task<AuditoriaSubastaReport> GetAuditoriaSubastaAsync(int idCotizacion, CancellationToken cancellationToken = default);
    Task<VerificacionDocumentacionReport> GetVerificacionDocumentacionAsync(int idCotizacion, CancellationToken cancellationToken = default);
}
