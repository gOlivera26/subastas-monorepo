namespace PortalSubastas.Providers.Application.Services;

public abstract class BaseService
{
    protected readonly DbContext _context = null!;
    protected readonly IMapper _mapper = null!;
    protected readonly IHttpContextAccessor _httpContextAccessor = null!;
    protected readonly IMemoryCache _cache = null!;
    private static ConcurrentDictionary<string, bool> _cacheKeys = new();

    protected BaseService() { }

    protected BaseService(IMemoryCache cache)
    {
        _cache = cache;
    }

    protected BaseService(DbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
    {
        _context = context;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    protected async Task<OperationResponse<List<TDto>>> FindByConditionAsyncLongCache<TEntity, TDto>(
        IQueryable<TEntity> query, string cacheKey)
        where TEntity : class
    {
        if (GetCache(cacheKey) is List<TDto> cachedData)
        {
            return Ok(cachedData);
        }

        var entityList = await query.AsNoTracking().ToListAsync();
        var entityDtoList = _mapper.Map<List<TDto>>(entityList);
        SetLongCache(cacheKey, entityDtoList);
        return Ok(entityDtoList);
    }

    protected async Task<OperationResponse<(List<TDto> Data, int Total)>> GetPagedDataAsync<TEntity, TDto>(
        int page, int pageSize, IQueryable<TEntity> query)
        where TEntity : class
    {
        var total = await query.CountAsync();

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<TDto>>(entities);

        return Ok((dtos, total), total);
    }

    protected async Task<OperationResponse<TDto>> InsertAsync<TEntity, TDto>(TDto dto)
        where TEntity : class
    {
        var entity = _mapper.Map<TEntity>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.Set<TEntity>().Add(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<TDto>(entity));
    }

    protected async Task<OperationResponse<TDto>> UpdateAsync<TEntity, TDto>(TEntity entity)
        where TEntity : class
    {
        PrepareAuditableEntity(entity, isNew: false);
        _context.Set<TEntity>().Update(entity);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<TDto>(entity));
    }

    protected async Task<OperationResponse<bool>> DeleteAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        PrepareAuditableEntity(entity, isNew: false, isDeleted: true);
        _context.Set<TEntity>().Update(entity);
        await _context.SaveChangesAsync();
        return Ok(true);
    }

    protected static OperationResponse<T> BadRequest<T>(string message, T? data = default)
        => OperationResponse<T>.CustomErrorResponse(400, message, data);

    protected static OperationResponse<T> Ok<T>(T data, int total = default)
        => OperationResponse<T>.SuccessResponse(data, total);

    protected static OperationResponse<T> OkMasive<T>(T data, int total = default)
        => OperationResponse<T>.SuccessResponseMassive(data, total);

    protected static OperationResponse<T> NotFound<T>()
        => OperationResponse<T>.NotFoundResponse();

    protected static OperationResponse<T> InternalServerError<T>(string exception)
        => OperationResponse<T>.ErrorResponse(exception);

    protected static OperationResponse<T> Unauthorized<T>(string message = "No autorizado")
        => OperationResponse<T>.UnauthorizedResponse(message);

    protected object? GetCache(string key) => _cache.Get(key);

    protected void SetCache<T>(string key, T data) => _cache?.Set(key, data, DateTime.Now.AddMinutes(2));

    private void SetLongCache<T>(string key, T data)
    {
        _cache?.Set(key, data, DateTime.Now.AddDays(4));
        _cacheKeys.TryAdd(key, true);
    }

    protected static IEnumerable<string> GetAllKeysCache() => _cacheKeys.Keys;

    protected bool RemoveAllKeysCache()
    {
        foreach (var key in _cacheKeys)
            RemoveKeyCache(key.Key);
        _cacheKeys.Clear();
        return _cacheKeys.IsEmpty;
    }

    protected bool RemoveKeyCache(string key)
    {
        var data = GetCache(key);
        if (data == null) return false;
        _cache?.Remove(key);
        _cacheKeys.TryRemove(key, out _);
        return true;
    }

    protected string GetCurrentUsername()
    {
        return _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "Sistema";
    }

    protected int? GetCurrentUserId()
    {
        var claimId = _httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claimId, out int id) ? id : null;
    }

    protected Guid? GetCurrentUserIdGuid()
    {
        var claimId = _httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claimId, out Guid id) ? id : null;
    }

    protected void PrepareAuditableEntity<T>(T entity, bool isNew, bool isDeleted = false)
    {
        var username = GetCurrentUsername();
        var zoneAr = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zoneAr);

        if (entity is IAuditableEntity auditable)
        {
            if (isNew)
            {
                auditable.UsrIng = username;
                auditable.FecIng = now;
            }
            else if (!isDeleted)
            {
                auditable.UsrMod = username;
                auditable.FecMod = now;
            }
        }

        if (isDeleted && entity is IFullAuditableEntity fullAuditable)
        {
            fullAuditable.UsrBaja = username;
            fullAuditable.FecBaja = now;
        }
    }

    protected async Task PublishSystemLogAsync(IPublishEndpoint publishEndpoint, string action, string module, object details)
    {
        var userId = GetCurrentUserIdGuid();
        var username = GetCurrentUsername();
        var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Desconocida";

        var logEvent = new SystemLogEvent(
            UserId: userId,
            Username: username,
            Action: action,
            Module: module,
            Details: JsonSerializer.Serialize(details),
            IpAddress: ipAddress,
            OccurredAt: TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"))
        );

        await publishEndpoint.Publish(logEvent);
    }
}
