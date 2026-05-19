using PortalSubastas.Providers.Application.RequestDto.Proveedor;
using PortalSubastas.Providers.Application.ResponseDto.Proveedor;

namespace PortalSubastas.Providers.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DomicilioController : BaseController
{
    private readonly IDomicilioService _domicilioService;
    private readonly ICatalogoService _catalogoService;

    public DomicilioController(IDomicilioService domicilioService, ICatalogoService catalogoService)
    {
        _domicilioService = domicilioService;
        _catalogoService = catalogoService;
    }

    [HttpGet("persona/{personaId}")]
    public async Task<IActionResult> GetByPersona(int personaId)
    {
        var result = await _domicilioService.GetDomiciliosByPersonaAsync(personaId);
        return Return(result);
    }

    [HttpPost("persona/{personaId}")]
    public async Task<IActionResult> Create(int personaId, [FromBody] CreateDomicilioDto dto)
    {
        var result = await _domicilioService.CreateAsync(personaId, dto);
        return Return(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateDomicilioDto dto)
    {
        var result = await _domicilioService.UpdateAsync(dto);
        return Return(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _domicilioService.DeleteAsync(id);
        return Return(result);
    }

    [HttpGet("tipos-domicilio")]
    public async Task<IActionResult> GetTiposDomicilio()
    {
        var result = await _catalogoService.GetTiposDomicilioAsync();
        return Return(result);
    }

    [HttpGet("provincias")]
    public async Task<IActionResult> GetProvincias()
    {
        var result = await _catalogoService.GetProvinciasAsync();
        return Return(result);
    }
}
