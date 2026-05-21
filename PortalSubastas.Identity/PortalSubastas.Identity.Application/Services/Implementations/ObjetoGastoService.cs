using PortalSubastas.Identity.Application.RequestDto.ObjetoGasto;
using PortalSubastas.Identity.Application.ResponseDto.ObjetoGasto;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class ObjetoGastoService : BaseService, IObjetoGastoService
{
    private readonly PortalSubastasContext _context;

    public ObjetoGastoService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<ObjetoGastoResponseDto>>> GetAllAsync(int? idVigencia)
    {
        var query = _context.TObjetosGasto
            .Include(o => o.IdVigenciaNavigation)
            .Include(o => o.IdOrganizacionNavigation)
            .AsQueryable();

        if (idVigencia.HasValue)
            query = query.Where(o => o.IdVigencia == idVigencia.Value);

        if (!IsSuperAdmin())
        {
            var orgId = GetUserOrganizationId();
            if (orgId.HasValue)
                query = query.Where(o => o.IdOrganizacion == null || o.IdOrganizacion == orgId.Value);
            else
                query = query.Where(o => o.IdOrganizacion == null);
        }

        var result = await query.OrderBy(o => o.NumeroObjeto).ToListAsync();
        return Ok(_mapper.Map<List<ObjetoGastoResponseDto>>(result));
    }

    public async Task<OperationResponse<ObjetoGastoResponseDto>> GetByIdAsync(int id)
    {
        var entity = await _context.TObjetosGasto
            .Include(o => o.IdVigenciaNavigation)
            .Include(o => o.IdOrganizacionNavigation)
            .FirstOrDefaultAsync(o => o.IdObjetoGasto == id);

        if (entity == null) return NotFound<ObjetoGastoResponseDto>();
        return Ok(_mapper.Map<ObjetoGastoResponseDto>(entity));
    }

    public async Task<OperationResponse<ObjetoGastoResponseDto>> CreateAsync(ObjetoGastoRequestDto dto)
    {
        var entity = _mapper.Map<TObjetoGasto>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.TObjetosGasto.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<ObjetoGastoResponseDto>(entity));
    }

    public async Task<OperationResponse<ObjetoGastoResponseDto>> UpdateAsync(int id, ObjetoGastoRequestDto dto)
    {
        var entity = await _context.TObjetosGasto.FindAsync(id);
        if (entity == null) return NotFound<ObjetoGastoResponseDto>();

        entity.IdObjetoGastoRel = dto.IdObjetoGastoRel;
        entity.NumeroObjeto = dto.NumeroObjeto;
        entity.NombreObjeto = dto.NombreObjeto;
        entity.IdVigencia = dto.IdVigencia;
        entity.IdOrganizacion = dto.IdOrganizacion;
        entity.ImputaEjecucion = dto.ImputaEjecucion;

        return await UpdateAsync<TObjetoGasto, ObjetoGastoResponseDto>(entity, _context);
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TObjetosGasto.FindAsync(id);
        if (entity == null) return NotFound<bool>();
        return await DeleteAsync(entity, _context);
    }
}
