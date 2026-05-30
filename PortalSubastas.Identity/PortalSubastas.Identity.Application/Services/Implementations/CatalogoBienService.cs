using PortalSubastas.Identity.Application.RequestDto.CatalogoBien;
using PortalSubastas.Identity.Application.ResponseDto.CatalogoBien;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class CatalogoBienService : BaseService, ICatalogoBienService
{
    private readonly PortalSubastasContext _context;

    public CatalogoBienService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache) { _context = context; }

    public async Task<OperationResponse<List<CatalogoBienResponseDto>>> GetAllAsync(int? idVigencia)
    {
        var query = _context.TCatalogosBien
            .Include(c => c.IdVigenciaNavigation)
            .Include(c => c.IdOrganizacionNavigation)
            .Include(c => c.IdObjetoGastoNavigation)
            .AsQueryable();

        if (idVigencia.HasValue)
            query = query.Where(c => c.IdVigencia == idVigencia.Value);

        if (!IsSuperAdmin())
        {
            var orgId = GetUserOrganizationId();
            if (orgId.HasValue)
                query = query.Where(c => c.IdOrganizacion == null || c.IdOrganizacion == orgId.Value);
            else
                query = query.Where(c => c.IdOrganizacion == null);
        }

        var items = await query.OrderBy(c => c.Codigo).ToListAsync();
        return Ok(_mapper.Map<List<CatalogoBienResponseDto>>(items));
    }

    public async Task<OperationResponse<CatalogoBienResponseDto>> GetByIdAsync(int id)
    {
        var entity = await _context.TCatalogosBien
            .Include(c => c.IdVigenciaNavigation)
            .Include(c => c.IdOrganizacionNavigation)
            .Include(c => c.IdObjetoGastoNavigation)
            .FirstOrDefaultAsync(c => c.IdItem == id);
        if (entity == null) return NotFound<CatalogoBienResponseDto>();
        return Ok(_mapper.Map<CatalogoBienResponseDto>(entity));
    }

    public async Task<OperationResponse<CatalogoBienResponseDto>> CreateAsync(CatalogoBienRequestDto dto)
    {
        var entity = _mapper.Map<TCatalogoBien>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.TCatalogosBien.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<CatalogoBienResponseDto>(entity));
    }

    public async Task<OperationResponse<CatalogoBienResponseDto>> UpdateAsync(int id, CatalogoBienRequestDto dto)
    {
        var entity = await _context.TCatalogosBien.FindAsync(id);
        if (entity == null) return NotFound<CatalogoBienResponseDto>();
        entity.IdItemRel = dto.IdItemRel; entity.Codigo = dto.Codigo; entity.NItem = dto.NItem;
        entity.IdVigencia = dto.IdVigencia; entity.IdOrganizacion = dto.IdOrganizacion; entity.IdObjetoGasto = dto.IdObjetoGasto;
        return await UpdateAsync<TCatalogoBien, CatalogoBienResponseDto>(entity, _context);
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TCatalogosBien.FindAsync(id);
        if (entity == null) return NotFound<bool>();
        return await DeleteAsync(entity, _context);
    }

    public async Task<OperationResponse<int>> UploadCsvAsync(CatalogoBienBulkUploadDto bulk)
    {
        var vigencia = await _context.TVigencias.FirstOrDefaultAsync(v => v.ActivoEjecucion == true);
        if (vigencia == null) return BadRequest<int>("No hay una vigencia activa en ejecución.");

        var objetosGasto = await _context.TObjetosGasto
            .Where(o => o.FecBaja == null)
            .ToDictionaryAsync(o => o.NumeroObjeto.Trim(), o => o.IdObjetoGasto);

        var count = 0;
        var errores = new List<string>();
        var existingCodigos = await _context.TCatalogosBien.Select(b => b.Codigo).ToListAsync();
        var usedCodigos = new HashSet<string>(existingCodigos);

        TCatalogoBien CreateEntity(CatalogoBienBulkItemDto item, int idObjetoGasto)
        {
            return new TCatalogoBien
            {
                IdItem = item.IdItem > 0 ? item.IdItem : 0,
                IdItemRel = item.IdItemRel,
                Codigo = item.Codigo,
                NItem = item.NItem,
                IdObjetoGasto = idObjetoGasto,
                IdVigencia = vigencia.IdVigencia,
                IdOrganizacion = bulk.IdOrganizacion,
                UsrIng = "UPLOAD"
            };
        }

        // Insertar todos sin FK primero, luego los que tienen FK
        // Pass 1: sin idItemRel (raíces)
        foreach (var item in bulk.Items.Where(i => i.IdItemRel == null || i.IdItemRel == 0))
        {
            var numeroObj = (item.NumeroObjeto ?? "").Replace("\"", "").Replace("'", "").Trim();
            if (!objetosGasto.TryGetValue(numeroObj, out var idObjetoGasto))
            { errores.Add($"codigo {item.Codigo}: objeto gasto '{numeroObj}' no encontrado"); continue; }
            if (!usedCodigos.Add(item.Codigo))
            { errores.Add($"codigo {item.Codigo}: duplicado"); continue; }

            var bien = CreateEntity(item, idObjetoGasto);
            _context.TCatalogosBien.Add(bien);
            count++;
        }
        await _context.SaveChangesAsync();

        // Pass 2: con idItemRel (hijos)
        foreach (var item in bulk.Items.Where(i => i.IdItemRel.HasValue && i.IdItemRel > 0))
        {
            if (!usedCodigos.Add(item.Codigo)) continue; // duplicado, skip

            var numeroObj = (item.NumeroObjeto ?? "").Replace("\"", "").Replace("'", "").Trim();
            if (!objetosGasto.TryGetValue(numeroObj, out var idObjetoGasto))
            { errores.Add($"codigo {item.Codigo}: objeto gasto '{numeroObj}' no encontrado"); continue; }

            var bien = CreateEntity(item, idObjetoGasto);
            _context.TCatalogosBien.Add(bien);
            count++;
        }
        await _context.SaveChangesAsync();

        if (errores.Any())
            return OperationResponse<int>.CreateBuilder()
                .WithSuccess(true).WithMessage($"Importados: {count}. Faltantes: {string.Join("; ", errores)}")
                .WithData(count).WithCode(200).Build();

        return Ok(count);
    }
}
