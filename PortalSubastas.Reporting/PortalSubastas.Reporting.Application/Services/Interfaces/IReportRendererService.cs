namespace PortalSubastas.Reporting.Application.Services.Interfaces;

public interface IReportRendererService
{
    Task<byte[]> RenderPdfFromHtmlAsync(
        string html,
        ReporteMetadata metadata,
        CancellationToken cancellationToken = default);
}
