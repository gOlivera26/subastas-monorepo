namespace PortalSubastas.Providers.Application.Services.Implementations;

public class CatalogoService : BaseService, ICatalogoService
{
    private readonly new ProvidersContext _context;

    public CatalogoService(ProvidersContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<TipoDomicilioDto>>> GetTiposDomicilioAsync()
    {
        var tipos = await _context.TTiposDomicilio
            .Where(t => t.FecBaja == null)
            .OrderBy(t => t.Descripcion)
            .ToListAsync();

        if (tipos.Count == 0)
            return NotFound<List<TipoDomicilioDto>>();

        return Ok(_mapper.Map<List<TipoDomicilioDto>>(tipos));
    }

    public async Task<OperationResponse<List<ProvinciaDto>>> GetProvinciasAsync()
    {
        var provincias = await _context.TProvincias
            .Where(p => p.FecBaja == null)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        if (provincias.Count == 0)
            return NotFound<List<ProvinciaDto>>();

        return Ok(_mapper.Map<List<ProvinciaDto>>(provincias));
    }
}
