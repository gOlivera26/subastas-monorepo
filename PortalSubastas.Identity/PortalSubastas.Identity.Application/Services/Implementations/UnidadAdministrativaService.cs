using PortalSubastas.Identity.Application.RequestDto.UnidadAdministrativa;
using PortalSubastas.Identity.Application.ResponseDto.UnidadAdministrativa;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class UnidadAdministrativaService : BaseService, IUnidadAdministrativaService
{
    private readonly PortalSubastasContext _context;

    public UnidadAdministrativaService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<UnidadAdministrativaResponseDto>>> GetByVigenciaAsync(int idVigencia)
    {
        var query = _context.TUnidadesAdministrativas
            .Where(u => u.IdVigencia == idVigencia)
            .Include(u => u.IdVigenciaNavigation)
            .Include(u => u.IdOrganizacionNavigation)
            .AsQueryable();

        if (!IsSuperAdmin())
        {
            var orgId = GetUserOrganizationId();
            if (orgId.HasValue)
                query = query.Where(u => u.IdOrganizacion == null || u.IdOrganizacion == orgId.Value);
            else
                query = query.Where(u => u.IdOrganizacion == null);
        }

        var unidades = await query.OrderBy(u => u.NumeroUnidadAdm).ToListAsync();
        return Ok(_mapper.Map<List<UnidadAdministrativaResponseDto>>(unidades));
    }

    public async Task<OperationResponse<UnidadAdministrativaResponseDto>> GetByIdAsync(int id)
    {
        var unidad = await _context.TUnidadesAdministrativas
            .Include(u => u.IdVigenciaNavigation)
            .Include(u => u.IdOrganizacionNavigation)
            .FirstOrDefaultAsync(u => u.IdUnidadAdm == id);

        if (unidad == null)
            return NotFound<UnidadAdministrativaResponseDto>();

        return Ok(_mapper.Map<UnidadAdministrativaResponseDto>(unidad));
    }

    public async Task<OperationResponse<UnidadAdministrativaResponseDto>> CreateAsync(UnidadAdministrativaRequestDto dto)
    {
        var entity = _mapper.Map<TUnidadAdministrativa>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.TUnidadesAdministrativas.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<UnidadAdministrativaResponseDto>(entity));
    }

    public async Task<OperationResponse<UnidadAdministrativaResponseDto>> UpdateAsync(int id, UnidadAdministrativaRequestDto dto)
    {
        var unidad = await _context.TUnidadesAdministrativas.FindAsync(id);
        if (unidad == null)
            return NotFound<UnidadAdministrativaResponseDto>();

        unidad.NumeroUnidadAdm = dto.NumeroUnidadAdm;
        unidad.NombreUnidadAdm = dto.NombreUnidadAdm;
        unidad.IdVigencia = dto.IdVigencia;
        unidad.IdOrganizacion = dto.IdOrganizacion;
        unidad.Mail = dto.Mail;
        unidad.Alias = dto.Alias;
        unidad.Puerto = dto.Puerto;
        unidad.Smtp = dto.Smtp;

        return await UpdateAsync<TUnidadAdministrativa, UnidadAdministrativaResponseDto>(unidad, _context);
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var unidad = await _context.TUnidadesAdministrativas.FindAsync(id);
        if (unidad == null)
            return NotFound<bool>();

        return await DeleteAsync(unidad, _context);
    }
}
