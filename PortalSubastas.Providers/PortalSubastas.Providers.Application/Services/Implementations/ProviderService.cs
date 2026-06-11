namespace PortalSubastas.Providers.Application.Services.Implementations;

public class ProviderService : BaseService, IProviderService
{
    private readonly new ProvidersContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IFileStorageService _fileStorage;

    public ProviderService(
        ProvidersContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IPublishEndpoint publishEndpoint,
        IFileStorageService fileStorage)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _fileStorage = fileStorage;
    }

    public async Task<OperationResponse<ProviderResponseDto>> VerifyCuitAsync(string cuit)
    {
        var proveedor = await _context.TProveedores.FirstOrDefaultAsync(p => p.Cuit == cuit);
        if (proveedor == null)
            return NotFound<ProviderResponseDto>();
        return Ok(_mapper.Map<ProviderResponseDto>(proveedor));
    }

    public async Task<OperationResponse<ProviderResponseDto>> GetByIdAsync(int id)
    {
        var proveedor = await _context.TProveedores.FirstOrDefaultAsync(p => p.Id == id);
        if (proveedor == null)
            return NotFound<ProviderResponseDto>();
        return Ok(_mapper.Map<ProviderResponseDto>(proveedor));
    }

    public async Task<OperationResponse<List<ProviderResponseDto>>> GetByIdsAsync(List<int> ids)
    {
        var normalizedIds = ids
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
            return BadRequest<List<ProviderResponseDto>>("Debe informar al menos un proveedor valido.");

        var proveedores = await _context.TProveedores
            .Where(p => normalizedIds.Contains(p.Id))
            .OrderBy(p => p.RazonSocial)
            .ToListAsync();

        if (proveedores.Count == 0)
            return NotFound<List<ProviderResponseDto>>();

        return Ok(_mapper.Map<List<ProviderResponseDto>>(proveedores));
    }

    public async Task<OperationResponse<ProviderListResponseDto>> GetProvidersAsync(int page, int pageSize, string? searchTerm, string? sortBy = null, string? sortDirection = null)
    {
        var query = _context.TProveedores.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.RazonSocial.ToLower().Contains(term) ||
                p.Cuit.Contains(term) ||
                p.Cup != null && p.Cup.Contains(term));
        }

        query = (sortBy, sortDirection?.ToLower()) switch
        {
            ("razonSocial", "desc") => query.OrderByDescending(p => p.RazonSocial),
            ("razonSocial", _) => query.OrderBy(p => p.RazonSocial),
            ("cuit", "desc") => query.OrderByDescending(p => p.Cuit),
            ("cuit", _) => query.OrderBy(p => p.Cuit),
            _ => query.OrderBy(p => p.RazonSocial)
        };

        query = query
            .Include(p => p.IdTipoPersonaNavigation)
            .Include(p => p.TProveedoresRubros);

        var result = await GetPagedDataAsync<TProveedore, ProviderListDto>(page, pageSize, query);
        var (data, total) = result.Data;

        if (total == 0)
            return NotFound<ProviderListResponseDto>();

        return Ok(new ProviderListResponseDto { Data = data, Total = total });
    }

    public async Task<OperationResponse<ProviderResponseDto>> CreateProviderAsync(CreateProviderDto dto)
    {
        var cuitExiste = await _context.TProveedores.AnyAsync(p => p.Cuit == dto.Cuit);
        if (cuitExiste)
            return BadRequest<ProviderResponseDto>("Ya existe un proveedor con ese CUIT.");

        var proveedor = _mapper.Map<TProveedore>(dto);

        PrepareAuditableEntity(proveedor, isNew: true);
        _context.TProveedores.Add(proveedor);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "PROVEEDOR_CREADO", "PROVEEDORES",
            new { ProveedorId = proveedor.Id, RazonSocial = proveedor.RazonSocial, Cuit = proveedor.Cuit });

        return Ok(_mapper.Map<ProviderResponseDto>(proveedor));
    }

    public async Task<OperationResponse<ProviderResponseDto>> UpdateProviderAsync(UpdateProviderDto dto)
    {
        var proveedor = await _context.TProveedores.FindAsync(dto.Id);
        if (proveedor == null)
            return NotFound<ProviderResponseDto>();

        var cuitExiste = await _context.TProveedores.AnyAsync(p => p.Cuit == dto.Cuit && p.Id != dto.Id);
        if (cuitExiste)
            return BadRequest<ProviderResponseDto>("Ya existe otro proveedor con ese CUIT.");

        _mapper.Map(dto, proveedor);

        PrepareAuditableEntity(proveedor, isNew: false);
        _context.TProveedores.Update(proveedor);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "PROVEEDOR_ACTUALIZADO", "PROVEEDORES",
            new { ProveedorId = proveedor.Id, RazonSocial = proveedor.RazonSocial });

        return Ok(_mapper.Map<ProviderResponseDto>(proveedor));
    }

    public async Task<OperationResponse<List<ProviderRubroDto>>> GetProviderRubrosAsync(int providerId)
    {
        var rubros = await _context.TProveedoresRubros
            .Where(pr => pr.IdProveedor == providerId && pr.FecBaja == null)
            .Include(pr => pr.IdRubroNavigation)
            .Select(pr => pr.IdRubroNavigation)
            .OrderBy(r => r.Codigo)
            .ToListAsync();

        if (rubros.Count == 0)
            return NotFound<List<ProviderRubroDto>>();

        return Ok(_mapper.Map<List<ProviderRubroDto>>(rubros));
    }

    public async Task<OperationResponse<bool>> LinkProviderRubrosAsync(int providerId, List<int> rubroIds)
    {
        var proveedor = await _context.TProveedores.FindAsync(providerId);
        if (proveedor == null)
            return NotFound<bool>();

        foreach (var rubroId in rubroIds)
        {
            var rubro = await _context.TRubros.FindAsync(rubroId);
            if (rubro == null)
                return BadRequest<bool>($"El rubro con ID {rubroId} no existe.");

            var existing = await _context.TProveedoresRubros
                .FirstOrDefaultAsync(pr => pr.IdProveedor == providerId && pr.IdRubro == rubroId && pr.FecBaja == null);

            if (existing == null)
            {
                var link = new TProveedoresRubro { IdProveedor = providerId, IdRubro = rubroId };
                PrepareAuditableEntity(link, isNew: true);
                _context.TProveedoresRubros.Add(link);
            }
        }

        await _context.SaveChangesAsync();
        await PublishSystemLogAsync(_publishEndpoint, "RUBRO_VINCULADO", "PROVEEDORES",
            new { ProveedorId = providerId, RubrosVinculados = rubroIds.Count });
        return Ok(true);
    }

    public async Task<OperationResponse<bool>> UnlinkProviderRubroAsync(int providerId, int rubroId)
    {
        var link = await _context.TProveedoresRubros
            .FirstOrDefaultAsync(pr => pr.IdProveedor == providerId && pr.IdRubro == rubroId && pr.FecBaja == null);

        if (link == null)
            return NotFound<bool>();

        PrepareAuditableEntity(link, isNew: false, isDeleted: true);
        _context.TProveedoresRubros.Update(link);
        await _context.SaveChangesAsync();
        await PublishSystemLogAsync(_publishEndpoint, "RUBRO_DESVINCULADO", "PROVEEDORES",
            new { ProveedorId = providerId, RubroId = rubroId });
        return Ok(true);
    }

    public async Task<OperationResponse<string>> UploadConstanciaAfipAsync(int providerId, Stream fileStream, string fileName, string contentType)
    {
        var proveedor = await _context.TProveedores.FindAsync(providerId);
        if (proveedor == null)
            return NotFound<string>();

        var url = await _fileStorage.UploadFileAsync(fileStream, fileName, contentType);

        proveedor.UrlConstanciaAfip = url;
        PrepareAuditableEntity(proveedor, isNew: false);
        _context.TProveedores.Update(proveedor);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "CONSTANCIA_AFIP_SUBIDA", "PROVEEDORES",
            new { ProveedorId = providerId, Url = url });

        return Ok(url);
    }
}
