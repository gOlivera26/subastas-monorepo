using PortalSubastas.Licitaciones.Application.RequestDto.Catalogos;
using PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class CategoriaProgramaticaService : BaseService, ICategoriaProgramaticaService
{
    private new readonly PortalSubastasContext _context;

    public CategoriaProgramaticaService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<CategoriaProgramaticaResponseDto>>> GetByFilterAsync(CategoriaProgramaticaFilterDto filtros)
    {
        filtros ??= new CategoriaProgramaticaFilterDto();

        var query = _context.TCategoriasProgramaticas.AsQueryable();

        if (filtros.Vigencia.HasValue)
        {
            query = query.Where(c => c.IdVigencia == filtros.Vigencia.Value);
        }

        if (filtros.UnidadAdm.HasValue)
        {
            query = query.Where(c => c.IdUnidadAdm == filtros.UnidadAdm.Value);
        }

        var entities = await query.AsNoTracking().ToListAsync();

        // Build CodigoCompleto by traversing hierarchy in memory
        var lookup = entities.ToDictionary(c => c.IdCatProg);
        var result = new List<CategoriaProgramaticaResponseDto>();

        foreach (var entity in entities.OrderBy(c => c.IdCatProg))
        {
            var dto = _mapper.Map<CategoriaProgramaticaResponseDto>(entity);
            dto.CodigoCompleto = BuildCodigoCompleto(entity, lookup);
            result.Add(dto);
        }

        return Ok(result.OrderBy(r => r.CodigoCompleto).ToList());
    }

    private static string BuildCodigoCompleto(TCategoriasProgramatica entity, Dictionary<int, TCategoriasProgramatica> lookup)
    {
        var parts = new List<string> { entity.Codigo.ToString() };
        var current = entity;

        while (current.IdCatProgRel.HasValue && lookup.TryGetValue(current.IdCatProgRel.Value, out var parent))
        {
            parts.Insert(0, parent.Codigo.ToString());
            current = parent;
        }

        return string.Join(".", parts);
    }
}
