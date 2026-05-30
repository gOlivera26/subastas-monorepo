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

    public async Task<OperationResponse<int>> UploadCsvAsync(ObjetoGastoBulkUploadDto bulk)
    {
        var vigencia = await _context.TVigencias.FirstOrDefaultAsync(v => v.ActivoEjecucion == true);
        if (vigencia == null) return BadRequest<int>("No hay una vigencia activa en ejecución.");

        var existingNumeros = await _context.TObjetosGasto.Select(o => o.NumeroObjeto).ToListAsync();
        var usedNumeros = new HashSet<string>(existingNumeros);

        var count = 0;
        var entities = new List<TObjetoGasto>();

        TObjetoGasto CreateEntity(ObjetoGastoBulkItemDto item)
        {
            return new TObjetoGasto
            {
                IdObjetoGasto = item.IdObjetoGasto > 0 ? item.IdObjetoGasto : 0,
                IdObjetoGastoRel = item.IdObjetoGastoRel,
                NumeroObjeto = item.NumeroObjeto,
                NombreObjeto = item.NombreObjeto,
                ImputaEjecucion = item.ImputaEjecucion ?? false,
                IdVigencia = vigencia.IdVigencia,
                IdOrganizacion = bulk.IdOrganizacion,
                UsrIng = "UPLOAD"
            };
        }

        foreach (var item in bulk.Items.Where(i => i.IdObjetoGastoRel == null || i.IdObjetoGastoRel == 0))
        {
            if (!usedNumeros.Add(item.NumeroObjeto)) continue;
            entities.Add(CreateEntity(item));
            count++;
        }

        await _context.TObjetosGasto.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        foreach (var item in bulk.Items.Where(i => i.IdObjetoGastoRel != null && i.IdObjetoGastoRel > 0))
        {
            if (!usedNumeros.Add(item.NumeroObjeto)) continue;
            entities.Add(CreateEntity(item));
            count++;
        }

        if (entities.Count > 0)
        {
            await _context.TObjetosGasto.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        return Ok(count);
    }
}
