using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IMonedaService
{
    Task<OperationResponse<List<MonedaResponseDto>>> GetAllAsync();
}
