namespace PortalSubastas.Providers.Application.Services.Implementations;

public class RubroService : BaseService, IRubroService
{
    private readonly new ProvidersContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public RubroService(
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

    public async Task<OperationResponse<RubroListResponseDto>> GetRubrosAsync(int page, int pageSize, string? searchTerm, string? sortBy = null, string? sortDirection = null)
    {
        var query = _context.TRubros.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(r =>
                r.Codigo.ToLower().Contains(term) ||
                r.Descripcion.ToLower().Contains(term));
        }

        query = (sortBy, sortDirection?.ToLower()) switch
        {
            ("codigo", "desc") => query.OrderByDescending(r => r.Codigo),
            ("codigo", _) => query.OrderBy(r => r.Codigo),
            ("descripcion", "desc") => query.OrderByDescending(r => r.Descripcion),
            ("descripcion", _) => query.OrderBy(r => r.Descripcion),
            _ => query.OrderBy(r => r.Codigo)
        };

        query = query.Include(r => r.IdRubroPadreNavigation);

        var result = await GetPagedDataAsync<TRubro, RubroListDto>(page, pageSize, query);
        var (data, total) = result.Data;

        return Ok(new RubroListResponseDto { Data = data, Total = total });
    }

    public async Task<OperationResponse<RubroListDto>> CreateRubroAsync(CreateRubroDto dto)
    {
        var codigoExiste = await _context.TRubros.AnyAsync(r => r.Codigo == dto.Codigo);
        if (codigoExiste)
            return BadRequest<RubroListDto>("Ya existe un rubro con ese código.");

        if (dto.IdRubroPadre.HasValue)
        {
            var padre = await _context.TRubros.FindAsync(dto.IdRubroPadre.Value);
            if (padre == null)
                return BadRequest<RubroListDto>("El rubro padre no existe.");
        }

        var rubro = _mapper.Map<TRubro>(dto);
        PrepareAuditableEntity(rubro, isNew: true);
        _context.TRubros.Add(rubro);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RUBRO_CREADO", "RUBROS",
            new { RubroId = rubro.Id, Codigo = rubro.Codigo, Descripcion = rubro.Descripcion });

        return Ok(_mapper.Map<RubroListDto>(rubro));
    }

    public async Task<OperationResponse<RubroListDto>> UpdateRubroAsync(UpdateRubroDto dto)
    {
        var rubro = await _context.TRubros.FindAsync(dto.Id);
        if (rubro == null)
            return NotFound<RubroListDto>();

        var codigoExiste = await _context.TRubros.AnyAsync(r => r.Codigo == dto.Codigo && r.Id != dto.Id);
        if (codigoExiste)
            return BadRequest<RubroListDto>("Ya existe otro rubro con ese código.");

        if (dto.IdRubroPadre.HasValue && dto.IdRubroPadre.Value == dto.Id)
            return BadRequest<RubroListDto>("Un rubro no puede ser su propio padre.");

        _mapper.Map(dto, rubro);
        PrepareAuditableEntity(rubro, isNew: false);
        _context.TRubros.Update(rubro);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RUBRO_ACTUALIZADO", "RUBROS",
            new { RubroId = rubro.Id, Codigo = rubro.Codigo });

        return Ok(_mapper.Map<RubroListDto>(rubro));
    }

    public async Task<OperationResponse<bool>> DeleteRubroAsync(int id)
    {
        var rubro = await _context.TRubros.FindAsync(id);
        if (rubro == null)
            return NotFound<bool>();

        var tieneHijos = await _context.TRubros.AnyAsync(r => r.IdRubroPadre == id && r.FecBaja == null);
        if (tieneHijos)
            return BadRequest<bool>("No se puede eliminar un rubro que tiene hijos. Elimine primero los rubros hijos.");

        var tieneProveedores = await _context.TProveedoresRubros.AnyAsync(pr => pr.IdRubro == id && pr.FecBaja == null);
        if (tieneProveedores)
            return BadRequest<bool>("No se puede eliminar un rubro que tiene proveedores vinculados.");

        PrepareAuditableEntity(rubro, isNew: false, isDeleted: true);
        _context.TRubros.Update(rubro);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "RUBRO_ELIMINADO", "RUBROS",
            new { RubroId = id, Codigo = rubro.Codigo });

        return Ok(true);
    }

    public async Task<OperationResponse<List<RubroTreeDto>>> GetRubroTreeAsync()
    {
        var rootRubros = await _context.TRubros
            .Where(r => r.IdRubroPadre == null && r.FecBaja == null)
            .OrderBy(r => r.Codigo)
            .ToListAsync();

        var tree = new List<RubroTreeDto>();
        foreach (var rubro in rootRubros)
            tree.Add(await BuildRubroTreeAsync(rubro, 0));

        return Ok(tree);
    }

    public async Task<OperationResponse<List<RubroTreeDto>>> GetRubroChildrenAsync(int parentId)
    {
        var children = await _context.TRubros
            .Where(r => r.IdRubroPadre == parentId && r.FecBaja == null)
            .OrderBy(r => r.Codigo)
            .ToListAsync();

        var result = new List<RubroTreeDto>();
        foreach (var child in children)
            result.Add(await BuildRubroTreeAsync(child, 0));

        return Ok(result);
    }

    public async Task<OperationResponse<List<RubroSearchResultDto>>> SearchRubrosAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest<List<RubroSearchResultDto>>("La busqueda requiere al menos un caracter.");

        var searchTerm = query.ToLower();
        var results = await _context.TRubros
            .Where(r => r.FecBaja == null &&
                        (r.Codigo.ToLower().Contains(searchTerm) || r.Descripcion.ToLower().Contains(searchTerm)))
            .OrderBy(r => r.Codigo)
            .Select(r => new RubroSearchResultDto
            {
                Id = r.Id,
                Codigo = r.Codigo,
                Descripcion = r.Descripcion,
                HasChildren = _context.TRubros.Any(c => c.IdRubroPadre == r.Id && c.FecBaja == null),
                Level = 0
            })
            .ToListAsync();

        return Ok(results);
    }

    private async Task<RubroTreeDto> BuildRubroTreeAsync(TRubro rubro, int depth)
    {
        var node = new RubroTreeDto
        {
            Id = rubro.Id,
            Codigo = rubro.Codigo,
            Descripcion = rubro.Descripcion,
            IdRubroPadre = rubro.IdRubroPadre,
            Imputable = rubro.Imputable,
            HasChildren = await _context.TRubros.AnyAsync(r => r.IdRubroPadre == rubro.Id && r.FecBaja == null)
        };

        if (depth < 2 && node.HasChildren)
        {
            var children = await _context.TRubros
                .Where(r => r.IdRubroPadre == rubro.Id && r.FecBaja == null)
                .OrderBy(r => r.Codigo)
                .ToListAsync();

            foreach (var child in children)
                node.Children.Add(await BuildRubroTreeAsync(child, depth + 1));
        }

        return node;
    }
}
