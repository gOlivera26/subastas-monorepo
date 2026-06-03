using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion.Consultas;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/Cotizacion/{idCotizacion:int}/[controller]")]
public class ConsultaController : BaseController
{
    private readonly IConsultaService _consultaService;

    public ConsultaController(IConsultaService consultaService)
    {
        _consultaService = consultaService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<ConsultaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int idCotizacion)
    {
        var result = await _consultaService.GetConsultasAsync(idCotizacion);
        return Return(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse<ConsultaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preguntar(int idCotizacion, [FromBody] PreguntaRequestDto dto)
    {
        var result = await _consultaService.RealizarPreguntaAsync(idCotizacion, dto);
        return Return(result);
    }

    [HttpPut("{idMensaje:int}/Responder")]
    [ProducesResponseType(typeof(OperationResponse<ConsultaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Responder(int idCotizacion, int idMensaje, [FromBody] RespuestaRequestDto dto)
    {
        var result = await _consultaService.ResponderPreguntaAsync(idCotizacion, idMensaje, dto);
        return Return(result);
    }
}