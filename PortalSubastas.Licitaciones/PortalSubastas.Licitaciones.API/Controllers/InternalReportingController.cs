using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("internal/reporting")]
public sealed class InternalReportingController : BaseController
{
    private readonly ICotizacionService _cotizacionService;

    public InternalReportingController(ICotizacionService cotizacionService)
    {
        _cotizacionService = cotizacionService;
    }

    [HttpGet("licitaciones/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<ReporteLicitacionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicitacion(int idCotizacion)
    {
        var result = await _cotizacionService.GetReporteLicitacionAsync(idCotizacion);
        return Return(result);
    }

    [HttpGet("acta-prelacion/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<ActaPrelacionReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActaPrelacion(int idCotizacion)
    {
        var result = await _cotizacionService.GetActaPrelacionAsync(idCotizacion);
        return Return(result);
    }

    [HttpGet("detalle-subasta/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<DetalleSubastaReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetalleSubasta(int idCotizacion)
    {
        var result = await _cotizacionService.GetDetalleSubastaAsync(idCotizacion);
        return Return(result);
    }

    [HttpGet("proveedores-invitados/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<ProveedoresInvitadosReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProveedoresInvitados(int idCotizacion)
    {
        var result = await _cotizacionService.GetProveedoresInvitadosAsync(idCotizacion);
        return Return(result);
    }

    [HttpGet("preguntas-respuestas/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<PreguntasRespuestasReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreguntasRespuestas(int idCotizacion)
    {
        var result = await _cotizacionService.GetPreguntasRespuestasAsync(idCotizacion);
        return Return(result);
    }

    [HttpGet("desistimiento/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<DesistimientoReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDesistimiento(int idCotizacion)
    {
        var result = await _cotizacionService.GetDesistimientoAsync(idCotizacion);
        return Return(result);
    }

    [HttpGet("observaciones-proveedores/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<ObservacionesProveedoresReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetObservacionesProveedores(int idCotizacion)
    {
        var result = await _cotizacionService.GetObservacionesProveedoresAsync(idCotizacion);
        return Return(result);
    }


    [HttpGet("verificacion-documentacion/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<VerificacionDocumentacionReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVerificacionDocumentacion(int idCotizacion)
    {
        var result = await _cotizacionService.GetVerificacionDocumentacionAsync(idCotizacion);
        return Return(result);
    }

    [HttpGet("auditoria-subasta/{idCotizacion:int}")]
    [ProducesResponseType(typeof(OperationResponse<AuditoriaSubastaReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditoriaSubasta(int idCotizacion)
    {
        var result = await _cotizacionService.GetAuditoriaSubastaAsync(idCotizacion);
        return Return(result);
    }
}
