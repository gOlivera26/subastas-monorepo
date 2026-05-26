using PortalSubastas.Licitaciones.Application.RequestDto.Ganador;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Ganador;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class GanadorService : BaseService, IGanadorService
{
    private readonly PortalSubastasContext _context;

    public GanadorService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<GanadorResponseDto>>> GetAllAsync(int idCotizacion)
    {
        var query = _context.TGanadores
            .Include(g => g.IdCotizacionNavigation)
            .Where(g => g.IdCotizacion == idCotizacion && g.FecBaja == null);

        var result = await query.OrderBy(g => g.MontoGanador).ToListAsync();
        return Ok(_mapper.Map<List<GanadorResponseDto>>(result));
    }

    public async Task<OperationResponse<GanadorResponseDto>> CreateAsync(GanadorRequestDto dto)
    {
        var entity = _mapper.Map<TGanador>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.TGanadores.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<GanadorResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TGanadores.FindAsync(id);
        if (entity == null) return NotFound<bool>();
        return await DeleteAsync(entity, _context);
    }
}
