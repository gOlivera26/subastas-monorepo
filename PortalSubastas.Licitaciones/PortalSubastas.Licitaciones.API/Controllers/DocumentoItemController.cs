using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Cotizacion/{idCotizacion:int}/DocumentoItem")]
public class DocumentoItemController : BaseController
{
    private readonly IDocumentoItemService _service;

    public DocumentoItemController(IDocumentoItemService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<DocumentoItemResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(int idCotizacion, [FromQuery] int? idCotizacionDetalle, [FromQuery] int? idRenglon)
    {
        var result = await _service.GetByItemAsync(idCotizacion, idCotizacionDetalle, idRenglon);
        return Return(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse<DocumentoItemResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(int idCotizacion, [FromForm] DocumentoItemRequestDto request)
    {
        request.IdCotizacion = idCotizacion;
        var result = await _service.UploadAsync(request);
        return Return(result);
    }

    [HttpDelete("{idDocItem:int}")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int idDocItem)
    {
        var result = await _service.DeleteAsync(idDocItem);
        return Return(result);
    }

    [HttpPost("Enviar")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Enviar(int idCotizacion, [FromQuery] int? idCotizacionDetalle, [FromQuery] int? idRenglon)
    {
        var result = await _service.EnviarDocumentacionDefinitivaAsync(idCotizacion, idCotizacionDetalle, idRenglon);
        return Return(result);
    }
}