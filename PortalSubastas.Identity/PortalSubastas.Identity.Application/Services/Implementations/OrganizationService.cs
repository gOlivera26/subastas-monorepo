namespace PortalSubastas.Identity.Application.Services.Implementations;

public class OrganizationService : BaseService, IOrganizationService
{
    private readonly PortalSubastasContext _context;

    public OrganizationService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<OrganizationResponseDto>>> GetAllActiveAsync()
    {
        var organizaciones = await _context.TOrganizaciones
            .Where(o => o.Activo == true)
            .OrderBy(o => o.Nombre)
            .ToListAsync();

        return Ok(_mapper.Map<List<OrganizationResponseDto>>(organizaciones));
    }
}
