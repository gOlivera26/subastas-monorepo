using PortalSubastas.Licitaciones.Application.RequestDto.Catalogos;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface ICategoriaProgramaticaService
{
    Task<OperationResponse<List<CategoriaProgramaticaResponseDto>>> GetByFilterAsync(CategoriaProgramaticaFilterDto filtros);
}
