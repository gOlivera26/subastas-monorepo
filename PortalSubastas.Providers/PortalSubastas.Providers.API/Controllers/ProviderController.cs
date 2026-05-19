using PortalSubastas.Providers.Application.RequestDto.Proveedor;
using PortalSubastas.Providers.Application.ResponseDto.Proveedor;

namespace PortalSubastas.Providers.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProviderController : BaseController
{
    private readonly IProviderService _providerService;
    private readonly IAfipService _afipService;

    public ProviderController(IProviderService providerService, IAfipService afipService)
    {
        _providerService = providerService;
        _afipService = afipService;
    }

    [HttpGet("verify/{cuit}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCuit(string cuit)
    {
        var result = await _providerService.VerifyCuitAsync(cuit);
        if (result.Success == false && result.Code == 404)
            return Return(OperationResponse<ProviderResponseDto>.CustomErrorResponse(404, "El CUIT ingresado no se encuentra empadronado como proveedor. Comuniquese con la administracion."));
        return Return(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetProviders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? q = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        var result = await _providerService.GetProvidersAsync(page, pageSize, q, sortBy, sortDirection);
        return Return(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProvider([FromBody] CreateProviderDto dto)
    {
        var result = await _providerService.CreateProviderAsync(dto);
        return Return(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProvider([FromBody] UpdateProviderDto dto)
    {
        var result = await _providerService.UpdateProviderAsync(dto);
        return Return(result);
    }

    [HttpGet("afip/verify/{cuit}")]
    public async Task<IActionResult> VerifyAfipCuit(string cuit)
    {
        var result = await _afipService.GetPersonDataAsync(cuit);
        return Return(result);
    }

    [HttpPost("{providerId}/constancia-afip")]
    public async Task<IActionResult> UploadConstanciaAfip(int providerId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Return(OperationResponse<string>.BadRequestResponse("No se selecciono ningun archivo."));

        using var stream = file.OpenReadStream();
        var result = await _providerService.UploadConstanciaAfipAsync(providerId, stream, file.FileName, file.ContentType);
        return Return(result);
    }

    [HttpGet("{providerId}/rubros")]
    public async Task<IActionResult> GetProviderRubros(int providerId)
    {
        var result = await _providerService.GetProviderRubrosAsync(providerId);
        return Return(result);
    }

    [HttpPost("{providerId}/rubros")]
    public async Task<IActionResult> LinkProviderRubros(int providerId, [FromBody] List<int> rubroIds)
    {
        var result = await _providerService.LinkProviderRubrosAsync(providerId, rubroIds);
        return Return(result);
    }

    [HttpDelete("{providerId}/rubros/{rubroId}")]
    public async Task<IActionResult> UnlinkProviderRubro(int providerId, int rubroId)
    {
        var result = await _providerService.UnlinkProviderRubroAsync(providerId, rubroId);
        return Return(result);
    }
}
