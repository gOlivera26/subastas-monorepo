using PortalSubastas.Licitaciones.Application.RequestDto.Catalogos;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface ICatalogoBienService
{
    Task<OperationResponse<List<CatalogoBienResponseDto>>> GetByFilterAsync(CatalogoBienFilterDto filtros);
}
