using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class ObjetoGastoService : BaseService, IObjetoGastoService
{
    private new readonly PortalSubastasContext _context;

    public ObjetoGastoService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<ObjetoGastoResponseDto>>> GetByFilterAsync(bool? vigente = null)
    {
        var query = _context.TObjetosGastos.AsQueryable();

        if (vigente == true)
        {
            query = query.Where(o => o.FecBaja == null);
        }

        query = query.OrderBy(o => o.NumeroObjeto);

        return await FindByConditionAsyncLongCache<TObjetosGasto, ObjetoGastoResponseDto>(
            query, $"ObjetoGasto_{vigente}");
    }
}
