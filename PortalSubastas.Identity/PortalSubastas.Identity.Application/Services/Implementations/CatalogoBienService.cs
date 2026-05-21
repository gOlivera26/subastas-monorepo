using PortalSubastas.Identity.Application.RequestDto.CatalogoBien;
using PortalSubastas.Identity.Application.ResponseDto.CatalogoBien;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class CatalogoBienService : BaseService, ICatalogoBienService
{
    private readonly PortalSubastasContext _context;

    public CatalogoBienService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

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

        var result = await query.OrderBy(c => c.Codigo).ToListAsync();
        return Ok(_mapper.Map<List<CatalogoBienResponseDto>>(result));
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

        entity.IdItemRel = dto.IdItemRel;
        entity.Codigo = dto.Codigo;
        entity.NItem = dto.NItem;
        entity.IdVigencia = dto.IdVigencia;
        entity.IdOrganizacion = dto.IdOrganizacion;
        entity.IdObjetoGasto = dto.IdObjetoGasto;

        return await UpdateAsync<TCatalogoBien, CatalogoBienResponseDto>(entity, _context);
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TCatalogosBien.FindAsync(id);
        if (entity == null) return NotFound<bool>();
        return await DeleteAsync(entity, _context);
    }
}
