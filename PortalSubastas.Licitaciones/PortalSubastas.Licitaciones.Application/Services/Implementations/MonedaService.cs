using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class MonedaService : BaseService, IMonedaService
{
    private new readonly PortalSubastasContext _context;

    public MonedaService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<MonedaResponseDto>>> GetAllAsync()
    {
        var query = _context.TMoneda
            .Where(m => m.FecBaja == null)
            .OrderBy(m => m.Nombre);

        return await FindByConditionAsyncLongCache<TMonedum, MonedaResponseDto>(query, "Moneda_All");
    }
}
