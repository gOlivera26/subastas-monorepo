using PortalSubastas.Reporting.Application.RequestDto;
using PortalSubastas.Reporting.Application.ResponseDto;
using PortalSubastas.Reporting.Application.Services.Interfaces;

namespace PortalSubastas.Reporting.Application.Services.Implementations;

public sealed class ReportService : IReportService
{
    private readonly IReportDataService _dataService;
    private readonly IReportTemplateService _templateService;
    private readonly IReportRendererService _rendererService;

    public ReportService(
        IReportDataService dataService,
        IReportTemplateService templateService,
        IReportRendererService rendererService)
    {
        _dataService = dataService;
        _templateService = templateService;
        _rendererService = rendererService;
    }

    public IReadOnlyCollection<ReporteCatalogoResponseDto> GetCatalogo()
    {
        return
        [
            new ReporteCatalogoResponseDto(
                Codigo: "acta-prelacion",
                Nombre: "Acta de prelacion de subasta",
                Descripcion: "Acta basada en la nueva logica de Subastas: cabecera, primera oferta reconstruida, historial y ganadores.",
                FormatosDisponibles: ["html", "pdf"]),
            new ReporteCatalogoResponseDto(
                Codigo: "detalle-subasta",
                Nombre: "Detalle de subasta",
                Descripcion: "Reporte ID 3 readecuado a la nueva logica: cabecera, items y proveedores participantes/invitados.",
                FormatosDisponibles: ["html", "pdf"]),
            new ReporteCatalogoResponseDto(
                Codigo: "proveedores-invitados",
                Nombre: "Listado de proveedores invitados",
                Descripcion: "Listado de proveedores asociados/invitados a una subasta con CUIT y datos de contacto disponibles.",
                FormatosDisponibles: ["html", "pdf"]),
            new ReporteCatalogoResponseDto(
                Codigo: "preguntas-respuestas",
                Nombre: "Preguntas y respuestas",
                Descripcion: "Reporte ID 4 readecuado a la nueva logica: consultas realizadas por proveedores y respuestas del organismo.",
                FormatosDisponibles: ["html", "pdf"]),
            new ReporteCatalogoResponseDto(
                Codigo: "desistimiento",
                Nombre: "Desistimiento de subasta",
                Descripcion: "Reporte ID 5 readecuado a la nueva logica: constancia de desistimiento y observaciones disponibles.",
                FormatosDisponibles: ["html", "pdf"]),
            new ReporteCatalogoResponseDto(
                Codigo: "observaciones-proveedores",
                Nombre: "Observaciones de proveedores",
                Descripcion: "Reporte ID 21 readecuado a la nueva logica: cabecera de impugnacion y observaciones disponibles.",
                FormatosDisponibles: ["html", "pdf"]),
            new ReporteCatalogoResponseDto(
                Codigo: "auditoria-subasta",
                Nombre: "Auditoria de subasta",
                Descripcion: "Reporte ID 10 readecuado a la nueva logica: resumen de documentos, ofertas y proveedores por subasta.",
                FormatosDisponibles: ["html", "pdf"]),
            new ReporteCatalogoResponseDto(
                Codigo: "verificacion-documentacion",
                Nombre: "Verificacion de documentacion",
                Descripcion: "Reporte ID 9 readecuado: documentacion y garantias presentadas en el modelo nuevo, sin inventar estados legacy.",
                FormatosDisponibles: ["html", "pdf"])
        ];
    }

    public async Task<string> GenerarLicitacionHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetActaPrelacionAsync(request.IdCotizacion, cancellationToken);
        return await _templateService.RenderActaPrelacionAsync(model);
    }

    public async Task<ReporteDocumentoResponseDto> GenerarLicitacionPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetActaPrelacionAsync(request.IdCotizacion, cancellationToken);
        var html = await _templateService.RenderActaPrelacionAsync(model);
        var metadata = new ReporteMetadata(
            TipoReporte.LicitacionDetalle,
            $"Acta de prelacion {model.Cabecera.NumeroCotizacion}",
            DateTimeOffset.UtcNow);

        var content = await _rendererService.RenderPdfFromHtmlAsync(html, metadata, cancellationToken);
        var fileName = $"Acta_Prelacion_{model.Cabecera.NumeroCotizacion.Replace("/", "-")}.pdf";

        return new ReporteDocumentoResponseDto(fileName, "application/pdf", content);
    }

    public async Task<string> GenerarDetalleSubastaHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetDetalleSubastaAsync(request.IdCotizacion, cancellationToken);
        return await _templateService.RenderDetalleSubastaAsync(model);
    }

    public async Task<ReporteDocumentoResponseDto> GenerarDetalleSubastaPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetDetalleSubastaAsync(request.IdCotizacion, cancellationToken);
        var html = await _templateService.RenderDetalleSubastaAsync(model);
        var metadata = new ReporteMetadata(
            TipoReporte.LicitacionDetalle,
            $"Detalle de subasta {model.Cabecera.NumeroCotizacion}",
            DateTimeOffset.UtcNow);

        var content = await _rendererService.RenderPdfFromHtmlAsync(html, metadata, cancellationToken);
        var fileName = $"Detalle_Subasta_{model.Cabecera.NumeroCotizacion.Replace("/", "-")}.pdf";

        return new ReporteDocumentoResponseDto(fileName, "application/pdf", content);
    }

    public async Task<string> GenerarProveedoresInvitadosHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetProveedoresInvitadosAsync(request.IdCotizacion, cancellationToken);
        return await _templateService.RenderProveedoresInvitadosAsync(model);
    }

    public async Task<ReporteDocumentoResponseDto> GenerarProveedoresInvitadosPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetProveedoresInvitadosAsync(request.IdCotizacion, cancellationToken);
        var html = await _templateService.RenderProveedoresInvitadosAsync(model);
        var metadata = new ReporteMetadata(
            TipoReporte.LicitacionDetalle,
            $"Proveedores invitados {model.Cabecera.NumeroCotizacion}",
            DateTimeOffset.UtcNow);

        var content = await _rendererService.RenderPdfFromHtmlAsync(html, metadata, cancellationToken);
        var fileName = $"Proveedores_Invitados_{model.Cabecera.NumeroCotizacion.Replace("/", "-")}.pdf";

        return new ReporteDocumentoResponseDto(fileName, "application/pdf", content);
    }

    public async Task<string> GenerarPreguntasRespuestasHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetPreguntasRespuestasAsync(request.IdCotizacion, cancellationToken);
        return await _templateService.RenderPreguntasRespuestasAsync(model);
    }

    public async Task<ReporteDocumentoResponseDto> GenerarPreguntasRespuestasPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetPreguntasRespuestasAsync(request.IdCotizacion, cancellationToken);
        var html = await _templateService.RenderPreguntasRespuestasAsync(model);
        var metadata = new ReporteMetadata(
            TipoReporte.LicitacionDetalle,
            $"Preguntas y respuestas {model.Cabecera.NumeroCotizacion}",
            DateTimeOffset.UtcNow);

        var content = await _rendererService.RenderPdfFromHtmlAsync(html, metadata, cancellationToken);
        var fileName = $"Preguntas_Respuestas_{model.Cabecera.NumeroCotizacion.Replace("/", "-")}.pdf";

        return new ReporteDocumentoResponseDto(fileName, "application/pdf", content);
    }

    public async Task<string> GenerarDesistimientoHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetDesistimientoAsync(request.IdCotizacion, cancellationToken);
        return await _templateService.RenderDesistimientoAsync(model);
    }

    public async Task<ReporteDocumentoResponseDto> GenerarDesistimientoPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetDesistimientoAsync(request.IdCotizacion, cancellationToken);
        var html = await _templateService.RenderDesistimientoAsync(model);
        var metadata = new ReporteMetadata(
            TipoReporte.LicitacionDetalle,
            $"Desistimiento {model.Cabecera.NumeroCotizacion}",
            DateTimeOffset.UtcNow);

        var content = await _rendererService.RenderPdfFromHtmlAsync(html, metadata, cancellationToken);
        var fileName = $"Desistimiento_{model.Cabecera.NumeroCotizacion.Replace("/", "-")}.pdf";

        return new ReporteDocumentoResponseDto(fileName, "application/pdf", content);
    }

    public async Task<string> GenerarObservacionesProveedoresHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetObservacionesProveedoresAsync(request.IdCotizacion, cancellationToken);
        return await _templateService.RenderObservacionesProveedoresAsync(model);
    }

    public async Task<ReporteDocumentoResponseDto> GenerarObservacionesProveedoresPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetObservacionesProveedoresAsync(request.IdCotizacion, cancellationToken);
        var html = await _templateService.RenderObservacionesProveedoresAsync(model);
        var metadata = new ReporteMetadata(
            TipoReporte.LicitacionDetalle,
            $"Observaciones de proveedores {model.Cabecera.NumeroCotizacion}",
            DateTimeOffset.UtcNow);

        var content = await _rendererService.RenderPdfFromHtmlAsync(html, metadata, cancellationToken);
        var fileName = $"Observaciones_Proveedores_{model.Cabecera.NumeroCotizacion.Replace("/", "-")}.pdf";

        return new ReporteDocumentoResponseDto(fileName, "application/pdf", content);
    }

    public async Task<string> GenerarAuditoriaSubastaHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetAuditoriaSubastaAsync(request.IdCotizacion, cancellationToken);
        return await _templateService.RenderAuditoriaSubastaAsync(model);
    }

    public async Task<ReporteDocumentoResponseDto> GenerarAuditoriaSubastaPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetAuditoriaSubastaAsync(request.IdCotizacion, cancellationToken);
        var html = await _templateService.RenderAuditoriaSubastaAsync(model);
        var metadata = new ReporteMetadata(
            TipoReporte.LicitacionDetalle,
            $"Auditoria de subasta {model.Cabecera.NumeroCotizacion}",
            DateTimeOffset.UtcNow);

        var content = await _rendererService.RenderPdfFromHtmlAsync(html, metadata, cancellationToken);
        var fileName = $"Auditoria_Subasta_{model.Cabecera.NumeroCotizacion.Replace("/", "-")}.pdf";

        return new ReporteDocumentoResponseDto(fileName, "application/pdf", content);
    }

    public async Task<string> GenerarVerificacionDocumentacionHtmlAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetVerificacionDocumentacionAsync(request.IdCotizacion, cancellationToken);
        return await _templateService.RenderVerificacionDocumentacionAsync(model);
    }

    public async Task<ReporteDocumentoResponseDto> GenerarVerificacionDocumentacionPdfAsync(
        ReporteLicitacionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var model = await _dataService.GetVerificacionDocumentacionAsync(request.IdCotizacion, cancellationToken);
        var html = await _templateService.RenderVerificacionDocumentacionAsync(model);
        var metadata = new ReporteMetadata(
            TipoReporte.LicitacionDetalle,
            $"Verificacion de documentacion {model.Cabecera.NumeroCotizacion}",
            DateTimeOffset.UtcNow);

        var content = await _rendererService.RenderPdfFromHtmlAsync(html, metadata, cancellationToken);
        var fileName = $"Verificacion_Documentacion_{model.Cabecera.NumeroCotizacion.Replace("/", "-")}.pdf";

        return new ReporteDocumentoResponseDto(fileName, "application/pdf", content);
    }

}
