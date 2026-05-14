using PortalSubastas.Identity.Application.ResponseDto.Proveedor;

namespace PortalSubastas.Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProviderController : BaseController
{
    private readonly IProviderService _providerService;

    public ProviderController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// Verifica si un proveedor está empadronado en el sistema mediante su CUIT.
    /// </summary>
    /// <param name="cuit">El CUIT del proveedor que se desea verificar (sin guiones).</param>
    /// <returns>Una respuesta HTTP que contiene los datos básicos del proveedor si existe, o un error personalizado 404 indicando que debe comunicarse con la administración.</returns>
    [HttpGet("verify/{cuit}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OperationResponse<ProviderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResponse<ProviderResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyCuit(string cuit)
    {
        var result = await _providerService.VerifyCuitAsync(cuit);

        if (result.Success == false && result.Code == 404)
        {
            return Return(OperationResponse<ProviderResponseDto>.CustomErrorResponse(404, "El CUIT ingresado no se encuentra empadronado como proveedor. Comuníquese con la administración."));
        }

        return Return(result);
    }
}