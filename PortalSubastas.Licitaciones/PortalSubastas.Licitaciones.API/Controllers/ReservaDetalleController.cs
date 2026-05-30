using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReservaDetalleController : ControllerBase
{
    private readonly IReservaDetalleService _detalleService;

    public ReservaDetalleController(IReservaDetalleService detalleService)
    {
        _detalleService = detalleService;
    }

    [HttpGet("reserva/{reservaId}")]
    [ProducesResponseType(typeof(OperationResponse<List<ReservaDetalleResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByReservaId(int reservaId)
    {
        var result = await _detalleService.GetByReservaIdAsync(reservaId);
        return result.Success == true ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OperationResponse<ReservaDetalleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _detalleService.GetByIdAsync(id);
        return result.Success == true ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse<ReservaDetalleResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] ReservaDetalleRequestDto dto)
    {
        var result = await _detalleService.CreateAsync(dto);
        return result.Success == true ? CreatedAtAction(nameof(GetById), new { id = result.Data!.IdReservaDet }, result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OperationResponse<ReservaDetalleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] ReservaDetalleRequestDto dto)
    {
        var result = await _detalleService.UpdateAsync(id, dto);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _detalleService.DeleteAsync(id);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/desautorizar")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Desautorizar(int id)
    {
        var result = await _detalleService.DesautorizarAsync(id);
        return result.Success == true ? Ok(result) : BadRequest(result);
    }

}
