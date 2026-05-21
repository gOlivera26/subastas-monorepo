using PortalSubastas.Identity.Application.RequestDto.CategoriaProgramatica;
using PortalSubastas.Identity.Application.ResponseDto.CategoriaProgramatica;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class CategoriaProgramaticaService : BaseService, ICategoriaProgramaticaService
{
    private readonly PortalSubastasContext _context;

    public CategoriaProgramaticaService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache) { _context = context; }

    public async Task<OperationResponse<List<CategoriaProgramaticaResponseDto>>> GetAllAsync(int? idVigencia)
    {
        var query = _context.TCategoriasProgramaticas.AsQueryable();
        if (idVigencia.HasValue) query = query.Where(c => c.IdVigencia == idVigencia.Value);
        if (!IsSuperAdmin()) { var orgId = GetUserOrganizationId(); if (orgId.HasValue) query = query.Where(c => c.IdOrganizacion == null || c.IdOrganizacion == orgId.Value); else query = query.Where(c => c.IdOrganizacion == null); }

        var flat = await query.Include(c => c.IdVigenciaNavigation).Include(c => c.IdOrganizacionNavigation).Include(c => c.IdUnidadAdmNavigation)
            .OrderBy(c => c.Codigo).ToListAsync();

        var result = BuildHierarchy(flat);
        return Ok(result);
    }

    private List<CategoriaProgramaticaResponseDto> BuildHierarchy(List<TCategoriaProgramatica> flat)
    {
        var map = flat.ToDictionary(c => c.IdCatProg);
        var result = new List<CategoriaProgramaticaResponseDto>();
        var visited = new HashSet<int>();
        var parentIds = new HashSet<int>(flat.Where(x => x.IdCatProgRel.HasValue).Select(x => x.IdCatProgRel.Value));

        void Walk(int id, int nivel)
        {
            if (!map.ContainsKey(id) || visited.Contains(id)) return;
            visited.Add(id);
            var c = map[id];
            result.Add(Map(c, nivel, parentIds));
            foreach (var child in flat.Where(x => x.IdCatProgRel == id).OrderBy(x => x.Codigo))
                Walk(child.IdCatProg, nivel + 1);
        }

        foreach (var root in flat.Where(c => c.IdCatProgRel == null).OrderBy(c => c.Codigo))
            Walk(root.IdCatProg, 0);

        return result;
    }

    private CategoriaProgramaticaResponseDto Map(TCategoriaProgramatica c, int nivel, HashSet<int>? parentIds = null)
    {
        return new CategoriaProgramaticaResponseDto
        {
            IdCatProg = c.IdCatProg, IdCatProgRel = c.IdCatProgRel,
            IdOrganizacion = c.IdOrganizacion, IdUnidadAdm = c.IdUnidadAdm,
            IdVigencia = c.IdVigencia, Codigo = c.Codigo,
            Nombre = c.Nombre, Naturaleza = c.Naturaleza,
            Nivel = nivel,
            HasChildren = parentIds?.Contains(c.IdCatProg) ?? false,
            NombreJerarquia = c.Nombre,
            OrganizacionNombre = c.IdOrganizacionNavigation?.Nombre,
            UnidadAdmNombre = c.IdUnidadAdmNavigation?.NombreUnidadAdm,
            VigenciaEjercicio = c.IdVigenciaNavigation?.Ejercicio.ToString()
        };
    }

    public async Task<OperationResponse<CategoriaProgramaticaResponseDto>> GetByIdAsync(int id)
    {
        var e = await _context.TCategoriasProgramaticas.Include(c => c.IdVigenciaNavigation).Include(c => c.IdOrganizacionNavigation).Include(c => c.IdUnidadAdmNavigation).FirstOrDefaultAsync(c => c.IdCatProg == id);
        return e == null ? NotFound<CategoriaProgramaticaResponseDto>() : Ok(Map(e, 0));
    }

    public async Task<OperationResponse<CategoriaProgramaticaResponseDto>> CreateAsync(CategoriaProgramaticaRequestDto dto)
    {
        var e = _mapper.Map<TCategoriaProgramatica>(dto);
        if (string.IsNullOrWhiteSpace(e.Naturaleza)) e.Naturaleza = null;
        PrepareAuditableEntity(e, isNew: true); _context.TCategoriasProgramaticas.Add(e); await _context.SaveChangesAsync();
        return Ok(Map(e, 0));
    }

    public async Task<OperationResponse<CategoriaProgramaticaResponseDto>> UpdateAsync(int id, CategoriaProgramaticaRequestDto dto)
    {
        var e = await _context.TCategoriasProgramaticas.FindAsync(id); if (e == null) return NotFound<CategoriaProgramaticaResponseDto>();
        e.IdCatProgRel = dto.IdCatProgRel; e.IdOrganizacion = dto.IdOrganizacion; e.IdUnidadAdm = dto.IdUnidadAdm; e.IdVigencia = dto.IdVigencia; e.Codigo = dto.Codigo; e.Nombre = dto.Nombre;
        e.Naturaleza = string.IsNullOrWhiteSpace(dto.Naturaleza) ? null : dto.Naturaleza;
        return await UpdateAsync<TCategoriaProgramatica, CategoriaProgramaticaResponseDto>(e, _context);
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id) { var e = await _context.TCategoriasProgramaticas.FindAsync(id); return e == null ? NotFound<bool>() : await DeleteAsync(e, _context); }
}
