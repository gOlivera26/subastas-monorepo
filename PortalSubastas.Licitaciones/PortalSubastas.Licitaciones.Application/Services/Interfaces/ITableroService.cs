using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Tablero;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface ITableroService
{
    Task<OperationResponse<TableroResponseDto>> GetAsync(
        int criterioAdjudicacion,
        int? idVigencia,
        CancellationToken cancellationToken = default);
}
