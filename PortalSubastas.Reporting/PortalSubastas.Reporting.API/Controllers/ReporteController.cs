using PortalSubastas.Reporting.Application.RequestDto;
using PortalSubastas.Reporting.Application.Services.Interfaces;

namespace PortalSubastas.Reporting.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class ReporteController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReporteController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("catalogo")]
    public IActionResult Catalogo()
    {
        var result = _reportService.GetCatalogo();
        return Ok(OperationResponse<object>.CreateBuilder()
            .WithSuccess(true)
            .WithCode(StatusCodes.Status200OK)
            .WithMessage("Catalogo de reportes disponible.")
            .WithData(result)
            .Build());
    }

    [HttpGet("licitaciones/{idCotizacion:int}/html")]
    public async Task<IActionResult> ObtenerLicitacionHtml(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var html = await _reportService.GenerarLicitacionHtmlAsync(request, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("licitaciones/{idCotizacion:int}/pdf")]
    public async Task<IActionResult> DescargarLicitacionPdf(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var report = await _reportService.GenerarLicitacionPdfAsync(request, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("subastas/{idCotizacion:int}/detalle/html")]
    public async Task<IActionResult> ObtenerDetalleSubastaHtml(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var html = await _reportService.GenerarDetalleSubastaHtmlAsync(request, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("subastas/{idCotizacion:int}/detalle/pdf")]
    public async Task<IActionResult> DescargarDetalleSubastaPdf(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var report = await _reportService.GenerarDetalleSubastaPdfAsync(request, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("subastas/{idCotizacion:int}/proveedores-invitados/html")]
    public async Task<IActionResult> ObtenerProveedoresInvitadosHtml(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var html = await _reportService.GenerarProveedoresInvitadosHtmlAsync(request, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("subastas/{idCotizacion:int}/proveedores-invitados/pdf")]
    public async Task<IActionResult> DescargarProveedoresInvitadosPdf(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var report = await _reportService.GenerarProveedoresInvitadosPdfAsync(request, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("subastas/{idCotizacion:int}/preguntas-respuestas/html")]
    public async Task<IActionResult> ObtenerPreguntasRespuestasHtml(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var html = await _reportService.GenerarPreguntasRespuestasHtmlAsync(request, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("subastas/{idCotizacion:int}/preguntas-respuestas/pdf")]
    public async Task<IActionResult> DescargarPreguntasRespuestasPdf(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var report = await _reportService.GenerarPreguntasRespuestasPdfAsync(request, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("subastas/{idCotizacion:int}/desistimiento/html")]
    public async Task<IActionResult> ObtenerDesistimientoHtml(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var html = await _reportService.GenerarDesistimientoHtmlAsync(request, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("subastas/{idCotizacion:int}/desistimiento/pdf")]
    public async Task<IActionResult> DescargarDesistimientoPdf(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var report = await _reportService.GenerarDesistimientoPdfAsync(request, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("subastas/{idCotizacion:int}/observaciones-proveedores/html")]
    public async Task<IActionResult> ObtenerObservacionesProveedoresHtml(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var html = await _reportService.GenerarObservacionesProveedoresHtmlAsync(request, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("subastas/{idCotizacion:int}/observaciones-proveedores/pdf")]
    public async Task<IActionResult> DescargarObservacionesProveedoresPdf(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var report = await _reportService.GenerarObservacionesProveedoresPdfAsync(request, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("subastas/{idCotizacion:int}/auditoria/html")]
    public async Task<IActionResult> ObtenerAuditoriaSubastaHtml(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var html = await _reportService.GenerarAuditoriaSubastaHtmlAsync(request, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("subastas/{idCotizacion:int}/auditoria/pdf")]
    public async Task<IActionResult> DescargarAuditoriaSubastaPdf(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var report = await _reportService.GenerarAuditoriaSubastaPdfAsync(request, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("subastas/{idCotizacion:int}/verificacion-documentacion/html")]
    public async Task<IActionResult> ObtenerVerificacionDocumentacionHtml(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var html = await _reportService.GenerarVerificacionDocumentacionHtmlAsync(request, cancellationToken);
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpGet("subastas/{idCotizacion:int}/verificacion-documentacion/pdf")]
    public async Task<IActionResult> DescargarVerificacionDocumentacionPdf(int idCotizacion, CancellationToken cancellationToken)
    {
        var request = new ReporteLicitacionRequestDto(idCotizacion);
        var report = await _reportService.GenerarVerificacionDocumentacionPdfAsync(request, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

}
