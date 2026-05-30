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

    public async Task<OperationResponse<int>> UploadCsvAsync(CategoriaProgramaticaBulkUploadDto bulk)
    {
        var vigencia = await _context.TVigencias.FirstOrDefaultAsync(v => v.ActivoEjecucion == true);
        if (vigencia == null) return BadRequest<int>("No hay una vigencia activa en ejecución.");

        var todasLasUas = await _context.TUnidadesAdministrativas.ToListAsync();
        var uaCache = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        var count = 0;

        int? MatchUA(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            var key = nombre.Trim().ToLower();
            if (uaCache.ContainsKey(key)) return uaCache[key];

            var replaceAccents = new string(key.Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());

            var match = todasLasUas.FirstOrDefault(u =>
            {
                var uName = (u.NombreUnidadAdm ?? "").ToLower();
                var uNameNoAccent = new string(uName.Normalize(System.Text.NormalizationForm.FormD)
                    .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    .ToArray());
                return uNameNoAccent.Contains(replaceAccents) || replaceAccents.Contains(uNameNoAccent);
            });

            uaCache[key] = match?.IdUnidadAdm;
            return uaCache[key];
        }

        var errores = new List<string>();
        foreach (var item in bulk.Items)
        {
            var uaId = MatchUA(item.NombreUnidadAdm);
            if (!uaId.HasValue)
            {
                errores.Add($"{item.Codigo} - {item.Nombre}: UA '{item.NombreUnidadAdm}' no encontrada");
                continue;
            }

            var cat = new TCategoriaProgramatica
            {
                Codigo = item.Codigo,
                Nombre = item.Nombre,
                Naturaleza = string.IsNullOrWhiteSpace(item.Naturaleza) ? null : item.Naturaleza,
                IdUnidadAdm = uaId.Value,
                IdVigencia = vigencia.IdVigencia,
                IdOrganizacion = bulk.IdOrganizacion
            };
            PrepareAuditableEntity(cat, isNew: true);
            _context.TCategoriasProgramaticas.Add(cat);
            count++;
        }
        await _context.SaveChangesAsync();

        if (errores.Any())
            return OperationResponse<int>.CreateBuilder()
                .WithSuccess(true)
                .WithMessage($"Importados: {count} de {bulk.Items.Count}. Faltantes: {string.Join("; ", errores)}")
                .WithData(count)
                .WithCode(200)
                .Build();

        return Ok(count);
    }
}
