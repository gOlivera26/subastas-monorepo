namespace PortalSubastas.Providers.Application.Services.Implementations;

public class DomicilioService : BaseService, IDomicilioService
{
    private readonly new ProvidersContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public DomicilioService(
        ProvidersContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IPublishEndpoint publishEndpoint)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OperationResponse<List<DomicilioDto>>> GetDomiciliosByPersonaAsync(int personaId)
    {
        var domicilios = await _context.TDomicilios
            .Where(d => d.IdPersona == personaId && d.FecBaja == null)
            .Include(d => d.IdTipoDomicilioNavigation)
            .Include(d => d.IdProvinciaNavigation)
            .OrderBy(d => d.IdTipoDomicilioNavigation.Descripcion)
            .ToListAsync();

        if (domicilios.Count == 0)
            return NotFound<List<DomicilioDto>>();

        return Ok(_mapper.Map<List<DomicilioDto>>(domicilios));
    }

    public async Task<OperationResponse<DomicilioDto>> CreateAsync(int personaId, CreateDomicilioDto dto)
    {
        var persona = await _context.TPersonas.FindAsync(personaId);
        if (persona == null)
            return NotFound<DomicilioDto>();

        var domicilio = _mapper.Map<TDomicilio>(dto);
        domicilio.IdPersona = personaId;

        PrepareAuditableEntity(domicilio, isNew: true);
        _context.TDomicilios.Add(domicilio);
        await _context.SaveChangesAsync();

        var domicilioCompleto = await _context.TDomicilios
            .Include(d => d.IdTipoDomicilioNavigation)
            .Include(d => d.IdProvinciaNavigation)
            .FirstAsync(d => d.Id == domicilio.Id);

        var result = _mapper.Map<DomicilioDto>(domicilioCompleto);

        await PublishSystemLogAsync(_publishEndpoint, "DOMICILIO_CREADO", "PROVEEDORES",
            new { PersonaId = personaId, DomicilioId = domicilio.Id, Tipo = result.TipoDomicilio });

        return Ok(result);
    }

    public async Task<OperationResponse<DomicilioDto>> UpdateAsync(UpdateDomicilioDto dto)
    {
        var domicilio = await _context.TDomicilios
            .FirstOrDefaultAsync(d => d.Id == dto.Id && d.FecBaja == null);

        if (domicilio == null)
            return NotFound<DomicilioDto>();

        _mapper.Map(dto, domicilio);

        PrepareAuditableEntity(domicilio, isNew: false);
        _context.TDomicilios.Update(domicilio);
        await _context.SaveChangesAsync();

        var domicilioCompleto = await _context.TDomicilios
            .Include(d => d.IdTipoDomicilioNavigation)
            .Include(d => d.IdProvinciaNavigation)
            .FirstAsync(d => d.Id == domicilio.Id);

        var result = _mapper.Map<DomicilioDto>(domicilioCompleto);

        await PublishSystemLogAsync(_publishEndpoint, "DOMICILIO_ACTUALIZADO", "PROVEEDORES",
            new { DomicilioId = domicilio.Id });

        return Ok(result);
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int domicilioId)
    {
        var domicilio = await _context.TDomicilios
            .FirstOrDefaultAsync(d => d.Id == domicilioId && d.FecBaja == null);

        if (domicilio == null)
            return NotFound<bool>();

        PrepareAuditableEntity(domicilio, isNew: false, isDeleted: true);
        _context.TDomicilios.Update(domicilio);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "DOMICILIO_ELIMINADO", "PROVEEDORES",
            new { DomicilioId = domicilioId });

        return Ok(true);
    }
}
