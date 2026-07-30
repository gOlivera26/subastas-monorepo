using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.API.Security;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Cotizacion/{idCotizacion:int}/Documento")]
public class CotizacionDocumentoController : BaseController
{
    private readonly ICotizacionDocumentoService _documentoService;
    private readonly IConfiguration _configuration;

    public CotizacionDocumentoController(ICotizacionDocumentoService documentoService, IConfiguration configuration)
    {
        _documentoService = documentoService;
        _configuration = configuration;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<CotizacionDocumentoResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int idCotizacion)
    {
        var result = await _documentoService.GetByCotizacionAsync(idCotizacion);
        return Return(result);
    }

    [HttpPost]
    [RequestSizeLimit(UploadSecurityPolicy.MaxDocumentBytes)]
    [ProducesResponseType(typeof(OperationResponse<CotizacionDocumentoResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(int idCotizacion, [FromForm] CotizacionDocumentoRequestDto request)
    {
        var validationError = await UploadSecurityPolicy.ValidateDocumentAsync(request.Archivo, _configuration);
        if (validationError is not null)
            return Return(OperationResponse<CotizacionDocumentoResponseDto>.BadRequestResponse(validationError));

        request.IdCotizacion = idCotizacion;
        var result = await _documentoService.CreateAsync(request);
        return Return(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int idCotizacion, int id)
    {
        var result = await _documentoService.DeleteAsync(id);
        return Return(result);
    }
}
