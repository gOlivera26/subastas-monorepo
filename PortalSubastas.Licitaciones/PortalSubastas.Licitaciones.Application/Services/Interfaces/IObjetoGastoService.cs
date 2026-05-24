using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IObjetoGastoService
{
    Task<OperationResponse<List<ObjetoGastoResponseDto>>> GetByFilterAsync(bool? vigente = null);
}
