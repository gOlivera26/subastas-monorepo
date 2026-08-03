namespace PortalSubastas.Reporting.Application.Services.Interfaces;

public interface IReportTemplateService
{
    Task<string> RenderLicitacionAsync(ReporteLicitacion model);
    Task<string> RenderActaPrelacionAsync(ActaPrelacionReport model);
    Task<string> RenderDetalleSubastaAsync(DetalleSubastaReport model);
    Task<string> RenderProveedoresInvitadosAsync(ProveedoresInvitadosReport model);
    Task<string> RenderPreguntasRespuestasAsync(PreguntasRespuestasReport model);
    Task<string> RenderDesistimientoAsync(DesistimientoReport model);
    Task<string> RenderObservacionesProveedoresAsync(ObservacionesProveedoresReport model);
    Task<string> RenderAuditoriaSubastaAsync(AuditoriaSubastaReport model);
    Task<string> RenderVerificacionDocumentacionAsync(VerificacionDocumentacionReport model);
}
