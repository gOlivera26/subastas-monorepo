using PortalSubastas.Identity.Application.RequestDto.SubResponsable;
using PortalSubastas.Identity.Application.ResponseDto.SubResponsable;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class SubResponsableService : BaseService, ISubResponsableService
{
    private readonly PortalSubastasContext _context;

    public SubResponsableService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache) { _context = context; }

    public async Task<OperationResponse<List<SubResponsableResponseDto>>> GetAllAsync(int? idUnidadAdm)
    {
        var query = _context.TSubResponsables.AsQueryable();
        if (idUnidadAdm.HasValue) query = query.Where(s => s.IdUnidadAdm == idUnidadAdm.Value);

        if (!IsSuperAdmin())
        {
            var orgId = GetUserOrganizationId();
            if (orgId.HasValue)
                query = query.Where(s => s.IdUnidadAdmNavigation != null && (s.IdUnidadAdmNavigation.IdOrganizacion == null || s.IdUnidadAdmNavigation.IdOrganizacion == orgId.Value));
            else
                query = query.Where(s => s.IdUnidadAdmNavigation != null && s.IdUnidadAdmNavigation.IdOrganizacion == null);
        }

        var flat = await query.Include(s => s.IdUnidadAdmNavigation)
            .OrderBy(s => s.Codigo).ToListAsync();

        var result = flat.Select(s => new SubResponsableResponseDto
        {
            IdSubResponsable = s.IdSubResponsable, Codigo = s.Codigo, Nombre = s.Nombre,
            IdSubRespRel = s.IdSubRespRel, Vigente = s.Vigente,
            IdUnidadAdm = s.IdUnidadAdm, UnidadAdmNombre = s.IdUnidadAdmNavigation?.NombreUnidadAdm
        }).ToList();

        return Ok(result);
    }

    public async Task<OperationResponse<SubResponsableResponseDto>> GetByIdAsync(int id)
    {
        var s = await _context.TSubResponsables.Include(s => s.IdUnidadAdmNavigation).FirstOrDefaultAsync(s => s.IdSubResponsable == id);
        if (s == null) return NotFound<SubResponsableResponseDto>();
        return Ok(MapToDto(s));
    }

    public async Task<OperationResponse<SubResponsableResponseDto>> CreateAsync(SubResponsableRequestDto dto)
    {
        var e = _mapper.Map<TSubResponsable>(dto);
        PrepareAuditableEntity(e, isNew: true);
        _context.TSubResponsables.Add(e); await _context.SaveChangesAsync();
        return Ok(MapToDto(e));
    }

    public async Task<OperationResponse<SubResponsableResponseDto>> UpdateAsync(int id, SubResponsableRequestDto dto)
    {
        var e = await _context.TSubResponsables.FindAsync(id);
        if (e == null) return NotFound<SubResponsableResponseDto>();
        e.Codigo = dto.Codigo; e.Nombre = dto.Nombre; e.IdSubRespRel = dto.IdSubRespRel;
        e.Vigente = dto.Vigente; e.IdUnidadAdm = dto.IdUnidadAdm;
        PrepareAuditableEntity(e, isNew: false);
        _context.TSubResponsables.Update(e); await _context.SaveChangesAsync();
        return Ok(MapToDto(e));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var e = await _context.TSubResponsables.FindAsync(id);
        if (e == null) return NotFound<bool>();
        PrepareAuditableEntity(e, isNew: false, isDeleted: true);
        _context.TSubResponsables.Update(e); await _context.SaveChangesAsync();
        return Ok(true);
    }

    public async Task<OperationResponse<int>> UploadCsvAsync(SubResponsableBulkUploadDto bulk)
    {
        var uaCache = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        var count = 0;
        foreach (var item in bulk.Items)
        {
            int? uaId = item.IdUnidadAdm;
            if (!uaId.HasValue && !string.IsNullOrWhiteSpace(item.NombreUnidadAdm))
            {
                var key = item.NombreUnidadAdm.Trim().ToLower();
                if (!uaCache.ContainsKey(key))
                {
                    var ua = await _context.TUnidadesAdministrativas
                        .Where(u => u.NombreUnidadAdm.ToLower().Contains(key))
                        .FirstOrDefaultAsync();
                    uaCache[key] = ua?.IdUnidadAdm;
                }
                uaId = uaCache[key];
            }
            if (!uaId.HasValue) continue;

            var sub = new TSubResponsable
            {
                Codigo = item.Codigo,
                Nombre = item.Nombre,
                IdUnidadAdm = uaId.Value
            };
            PrepareAuditableEntity(sub, isNew: true);
            _context.TSubResponsables.Add(sub);
            count++;
        }
        await _context.SaveChangesAsync();
        return Ok(count);
    }

    private static SubResponsableResponseDto MapToDto(TSubResponsable s) => new()
    {
        IdSubResponsable = s.IdSubResponsable, Codigo = s.Codigo, Nombre = s.Nombre,
        IdSubRespRel = s.IdSubRespRel, Vigente = s.Vigente,
        IdUnidadAdm = s.IdUnidadAdm, UnidadAdmNombre = s.IdUnidadAdmNavigation?.NombreUnidadAdm
    };
}
