using PortalSubastas.Providers.Application.ResponseDto.Proveedor;

namespace PortalSubastas.Providers.Application.Services.Interfaces;

public interface ICatalogoService
{
    Task<OperationResponse<List<TipoDomicilioDto>>> GetTiposDomicilioAsync();
    Task<OperationResponse<List<ProvinciaDto>>> GetProvinciasAsync();
    Task<OperationResponse<List<TipoPersonaDto>>> GetTiposPersonaAsync();
}
