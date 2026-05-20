using PortalSubastas.Identity.Application.RequestDto.Organizacion;
using PortalSubastas.Identity.Application.ResponseDto.Organizacion;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class OrganizationService : BaseService, IOrganizationService
{
    private readonly PortalSubastasContext _context;

    public OrganizationService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<OrganizationResponseDto>>> GetAllAsync()
    {
        var organizaciones = await _context.TOrganizaciones
            .OrderBy(o => o.Nombre)
            .ToListAsync();

        return Ok(_mapper.Map<List<OrganizationResponseDto>>(organizaciones));
    }

    public async Task<OperationResponse<List<OrganizationResponseDto>>> GetAllActiveAsync()
    {
        var organizaciones = await _context.TOrganizaciones
            .Where(o => o.Activo == true)
            .OrderBy(o => o.Nombre)
            .ToListAsync();

        return Ok(_mapper.Map<List<OrganizationResponseDto>>(organizaciones));
    }

    public async Task<OperationResponse<OrganizationResponseDto>> GetByIdAsync(int id)
    {
        var organizacion = await _context.TOrganizaciones.FindAsync(id);
        if (organizacion == null)
            return NotFound<OrganizationResponseDto>();

        return Ok(_mapper.Map<OrganizationResponseDto>(organizacion));
    }

    public async Task<OperationResponse<OrganizationResponseDto>> CreateAsync(OrganizationRequestDto dto)
    {
        var entity = _mapper.Map<TOrganizacione>(dto);
        _context.TOrganizaciones.Add(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<OrganizationResponseDto>(entity));
    }

    public async Task<OperationResponse<OrganizationResponseDto>> UpdateAsync(int id, OrganizationRequestDto dto)
    {
        var organizacion = await _context.TOrganizaciones.FindAsync(id);
        if (organizacion == null)
            return NotFound<OrganizationResponseDto>();

        organizacion.Nombre = dto.Nombre;
        organizacion.Cuit = dto.Cuit;
        organizacion.Abreviatura = dto.Abreviatura;
        organizacion.Activo = dto.Activo;

        _context.TOrganizaciones.Update(organizacion);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<OrganizationResponseDto>(organizacion));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var organizacion = await _context.TOrganizaciones.FindAsync(id);
        if (organizacion == null)
            return NotFound<bool>();

        // TOrganizacione doesn't use IFullAuditableEntity, so we just set Activo = false for soft delete
        organizacion.Activo = false;
        _context.TOrganizaciones.Update(organizacion);
        await _context.SaveChangesAsync();
        
        return Ok(true);
    }
}
