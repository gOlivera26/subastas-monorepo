using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CotizacionController : BaseController
{
    private readonly ICotizacionService _cotizacionService;

    public CotizacionController(ICotizacionService cotizacionService)
    {
        _cotizacionService = cotizacionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(OperationResponse<List<CotizacionResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? idVigencia)
    {
        var result = await _cotizacionService.GetAllAsync(idVigencia);
        return Return(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OperationResponse<CotizacionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _cotizacionService.GetByIdAsync(id);
        return Return(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OperationResponse<CotizacionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CotizacionRequestDto dto)
    {
        var result = await _cotizacionService.CreateAsync(dto);
        return Return(result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(OperationResponse<CotizacionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] CotizacionRequestDto dto)
    {
        var result = await _cotizacionService.UpdateAsync(id, dto);
        return Return(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(OperationResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _cotizacionService.DeleteAsync(id);
        return Return(result);
    }

    // --- Endpoints Dashboard ---
    [HttpGet("dashboard/en-curso")]
    [ProducesResponseType(typeof(OperationResponse<List<SubastaDashboardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubastasEnCurso([FromQuery] int? idVigencia)
    {
        var result = await _cotizacionService.GetSubastasEnCursoAsync(idVigencia);
        return Return(result);
    }

    [HttpGet("dashboard/proximas")]
    [ProducesResponseType(typeof(OperationResponse<List<SubastaDashboardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubastasProximas([FromQuery] int? idVigencia)
    {
        var result = await _cotizacionService.GetSubastasProximasAsync(idVigencia);
        return Return(result);
    }

    [HttpGet("dashboard/del-mes")]
    [ProducesResponseType(typeof(OperationResponse<List<SubastaDashboardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubastasDelMes([FromQuery] int? idVigencia)
    {
        var result = await _cotizacionService.GetSubastasDelMesAsync(idVigencia);
        return Return(result);
    }

    // --- Búsqueda con filtros ---
    [HttpGet("buscar")]
    [ProducesResponseType(typeof(OperationResponse<List<SubastaDashboardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Buscar([FromQuery] int? idVigencia, [FromQuery] int? idEstado, [FromQuery] string? nro = null, [FromQuery] string? expte = null, [FromQuery] DateTime? fechaDesde = null)
    {
        var result = await _cotizacionService.BuscarAsync(idVigencia, idEstado, nro, expte, fechaDesde);
        return Return(result);
    }

    // --- Transiciones de estado ---
    [HttpPost("{id:int}/notificar")]
    [ProducesResponseType(typeof(OperationResponse<CotizacionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Notificar(int id)
    {
        var result = await _cotizacionService.NotificarAsync(id);
        return Return(result);
    }

    [HttpPost("{id:int}/finalizar")]
    [ProducesResponseType(typeof(OperationResponse<CotizacionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Finalizar(int id)
    {
        var result = await _cotizacionService.FinalizarAsync(id);
        return Return(result);
    }

    [HttpPost("{id:int}/desistir")]
    [ProducesResponseType(typeof(OperationResponse<CotizacionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Desistir(int id)
    {
        var result = await _cotizacionService.DesistirAsync(id);
        return Return(result);
    }

    [HttpPost("{id:int}/prorrogar")]
    [ProducesResponseType(typeof(OperationResponse<CotizacionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Prorrogar(int id, [FromBody] ProrrogaRequestDto dto)
    {
        var result = await _cotizacionService.ProrrogarAsync(id, dto.Minutos);
        return Return(result);
    }
}
