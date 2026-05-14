using PortalSubastas.Identity.Application.ResponseDto;
using PortalSubastas.Identity.Application.ResponseDto.Proveedor;

namespace PortalSubastas.Identity.Application.Services.Implementations;

public class ProviderService : BaseService, IProviderService
{
    private readonly PortalSubastasContext _context;

    public ProviderService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<ProviderResponseDto>> VerifyCuitAsync(string cuit)
    {
        var proveedor = await _context.TProveedores
            .FirstOrDefaultAsync(p => p.Cuit == cuit);

        if (proveedor == null)
        {
            return NotFound<ProviderResponseDto>();
        }
        return Ok(_mapper.Map<ProviderResponseDto>(proveedor));
    }
}