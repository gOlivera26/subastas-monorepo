using PortalSubastas.Licitaciones.Application.RequestDto.Catalogos;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class CatalogoBienService : BaseService, ICatalogoBienService
{
    private new readonly PortalSubastasContext _context;

    public CatalogoBienService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<CatalogoBienResponseDto>>> GetByFilterAsync(CatalogoBienFilterDto filtros)
    {
        filtros ??= new CatalogoBienFilterDto();

        var query = _context.TCatalogosBiens.AsQueryable();

        if (filtros.Vigente == true)
        {
            query = query.Where(b => b.FecBaja == null);
        }

        if (filtros.Jurisdiccion.HasValue)
        {
            query = query.Where(b => b.IdOrganizacion == filtros.Jurisdiccion.Value);
        }

        if (filtros.CategoriaBien.HasValue)
        {
            query = query.Where(b => b.IdObjetoGasto == filtros.CategoriaBien.Value);
        }

        query = query.OrderBy(b => b.NItem);

        return await FindByConditionAsyncLongCache<TCatalogosBien, CatalogoBienResponseDto>(
            query, $"CatalogoBien_{filtros.Vigente}_{filtros.Jurisdiccion}_{filtros.CategoriaBien}");
    }
}
