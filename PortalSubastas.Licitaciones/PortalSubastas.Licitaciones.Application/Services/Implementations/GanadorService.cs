using PortalSubastas.Licitaciones.Application.RequestDto.Ganador;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Ganador;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class GanadorService : BaseService, IGanadorService
{
    private readonly PortalSubastasContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public GanadorService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache, IPublishEndpoint publishEndpoint)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
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

        await PublishSystemLogAsync(_publishEndpoint, "GANADOR_REGISTRADO", "LICITACIONES", new { entity.IdGanador, entity.IdCotizacion, entity.MontoGanador });

        return Ok(_mapper.Map<GanadorResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TGanadores.FindAsync(id);
        if (entity == null) return NotFound<bool>();
        var result = await DeleteAsync(entity, _context);
        if (result.Success == true)
            await PublishSystemLogAsync(_publishEndpoint, "GANADOR_ELIMINADO", "LICITACIONES", new { entity.IdGanador, entity.IdCotizacion });
        return result;
    }
}
