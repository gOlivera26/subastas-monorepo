using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Reserva;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Estados;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reserva;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReservaController : ControllerBase
{
    private readonly IReservaService _reservaService;

    public ReservaController(IReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<ReservaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ReservaFilterDto? filtros = null)
    {
        var result = await _reservaService.GetAllAsync(filtros);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OperationResponse<ReservaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _reservaService.GetByIdAsync(id);
        return result.Success == true ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse<ReservaResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] ReservaRequestDto dto)
    {
        var result = await _reservaService.CreateAsync(dto);
        return result.Success == true ? CreatedAtAction(nameof(GetById), new { id = result.Data!.IdReserva }, result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OperationResponse<ReservaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] ReservaRequestDto dto)
    {
        var result = await _reservaService.UpdateAsync(id, dto);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _reservaService.DeleteAsync(id);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/autorizar")]
    [ProducesResponseType(typeof(OperationResponse<ReservaResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Autorizar(int id, [FromBody] AutorizarReservaDto dto)
    {
        var result = await _reservaService.AutorizarAsync(id, dto);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/clonar")]
    [ProducesResponseType(typeof(OperationResponse<ReservaResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Clonar(int id)
    {
        var result = await _reservaService.ClonarAsync(id);
        return result.Success == true ? CreatedAtAction(nameof(GetById), new { id = result.Data!.IdReserva }, result) : BadRequest(result);
    }

    [HttpGet("estados")]
    [ProducesResponseType(typeof(OperationResponse<List<EstadoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEstados()
    {
        var result = await _reservaService.GetEstadosAsync();
        return result.Success == true ? Ok(result) : BadRequest(result);
    }
}
