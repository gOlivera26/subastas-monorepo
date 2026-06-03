using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Cotizacion/{idCotizacion:int}/Documento")]
public class CotizacionDocumentoController : BaseController
{
    private readonly ICotizacionDocumentoService _documentoService;

    public CotizacionDocumentoController(ICotizacionDocumentoService documentoService)
    {
        _documentoService = documentoService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<CotizacionDocumentoResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int idCotizacion)
    {
        var result = await _documentoService.GetByCotizacionAsync(idCotizacion);
        return Return(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse<CotizacionDocumentoResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(int idCotizacion, [FromForm] CotizacionDocumentoRequestDto request)
    {
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