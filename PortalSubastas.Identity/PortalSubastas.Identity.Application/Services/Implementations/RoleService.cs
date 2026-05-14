using PortalSubastas.Identity.Application.ResponseDto.Role;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class RoleService : BaseService, IRoleService
{
    private readonly PortalSubastasContext _context;

    public RoleService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<RoleResponseDto>>> GetActiveRolesAsync()
    {
        var roles = await _context.TRoles.ToListAsync();
        return Ok(_mapper.Map<List<RoleResponseDto>>(roles));
    }
}
