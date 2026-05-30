namespace PortalSubastas.Identity.Application.Services;

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
        else
        {
            var entityList = await query.AsNoTracking().ToListAsync();
            var entityDtoList = _mapper.Map<List<TDto>>(entityList);
            SetLongCache(cacheKey, entityDtoList);
            return Ok(entityDtoList);
        }
    }

    protected static OperationResponse<T> BadRequest<T>(string message, T? data = default)
        => OperationResponse<T>.CustomErrorResponse(400, message, data);

    protected static OperationResponse<T> Ok<T>(T data, int total = default)
        => OperationResponse<T>.SuccessResponse(data, total);

    protected static OperationResponse<T> OkMasive<T>(T data, int total = default)
        => OperationResponse<T>.SuccessResponseMassive(data, total);

    protected static OperationResponse<T> NotFound<T>()
        => OperationResponse<T>.NotFoundResponse();

    protected static OperationResponse<T> ServerErrorFile<T>(string exception, object exceptionDetails)
        => OperationResponse<T>.ErrorFileResponse(exception, exceptionDetails);

    protected static OperationResponse<T> InternalServerError<T>(string exception)
        => OperationResponse<T>.ErrorResponse(exception);

    protected static OperationResponse<T> Unauthorized<T>(string message = "No autorizado")
        => OperationResponse<T>.UnauthorizedResponse(message);

    protected object GetCache(string key)
    {
        var data = _cache.Get(key);
        return data;
    }

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
        {
            RemoveKeyCache(key.Key);
        }

        _cacheKeys.Clear();
        return _cacheKeys.IsEmpty;
    }

    protected bool RemoveKeyCache(string key)
    {
        var data = GetCache(key);
        if (data == null)
        {
            return false;
        }

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

    protected int? GetUserOrganizationId()
    {
        var claim = _httpContextAccessor?.HttpContext?.User?.FindFirst("IdOrganizacion");
        return claim != null && int.TryParse(claim.Value, out int orgId) ? orgId : null;
    }

    protected bool IsSuperAdmin()
    {
        return _httpContextAccessor?.HttpContext?.User?.IsInRole("SUPERADMIN") == true;
    }

    protected void PrepareAuditableEntity<T>(T entity, bool isNew, bool isDeleted = false)
    {
        var username = GetCurrentUsername();
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

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
            OccurredAt: DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        );

        await publishEndpoint.Publish(logEvent);
    }

    protected async Task<OperationResponse<(List<TDto> Data, int Total)>> GetPagedDataAsync<TEntity, TDto>(int page, int pageSize,
        IQueryable<TEntity> query)
    {
        var total = await query.CountAsync();

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<TDto>>(entities);

        return Ok((dtos, total), total);
    }

    protected int GetJurisdiccionCache()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        return int.Parse(user?.Claims.FirstOrDefault(c => c.Type == "IdJurisdiccion")?.Value ?? "0");
    }

    protected async Task<OperationResponse<TDto>> InsertAsync<TEntity, TDto>(TDto dto, DbContext context)
        where TEntity : class
    {
        var entity = _mapper.Map<TEntity>(dto);

        PrepareAuditableEntity(entity, isNew: true);

        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync();

        return Ok(_mapper.Map<TDto>(entity));
    }

    protected async Task<OperationResponse<List<TDto>>> InsertEntitesAsync<TEntity, TDto>(List<TDto> dtos,
        DbContext context)
        where TEntity : class
    {
        var entities = _mapper.Map<List<TEntity>>(dtos);

        foreach (var entity in entities)
        {
            await context.Set<TEntity>().AddAsync(entity);
        }

        await context.SaveChangesAsync();

        return Ok(_mapper.Map<List<TDto>>(entities));
    }

    protected async Task<OperationResponse<List<TDto>>> UpdateEntitiesAsync<TEntity, TDto>(List<TEntity> entities,
        DbContext context)
        where TEntity : class
    {
        foreach (var entity in entities)
        {
            context.Set<TEntity>().Update(entity);
        }

        await context.SaveChangesAsync();

        return Ok(_mapper.Map<List<TDto>>(entities));
    }

    protected async Task<OperationResponse<TDto>> UpdateAsync<TEntity, TDto>(TEntity entity, DbContext context)
        where TEntity : class
    {
        PrepareAuditableEntity(entity, isNew: false);

        context.Set<TEntity>().Update(entity);
        await context.SaveChangesAsync();

        return Ok(_mapper.Map<TDto>(entity));
    }

    protected static async Task<OperationResponse<bool>> InsertWithTransactionAsync(Func<Task> insertActionsAsync,
        DbContext context)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await insertActionsAsync();

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return OperationResponse<bool>.ErrorResponse(ex.Message);
        }
    }

    protected async Task<OperationResponse<List<TDto>>> DeleteEntitiesAsync<TEntity, TDto>(List<TEntity> entities,
        DbContext context)
        where TEntity : class
    {
        foreach (var entity in entities)
        {
            PrepareAuditableEntity(entity, isNew: false, isDeleted: true);
            context.Set<TEntity>().Update(entity);
        }

        await context.SaveChangesAsync();

        return Ok(_mapper.Map<List<TDto>>(entities));
    }

    protected async Task<OperationResponse<bool>> DeleteAsync<TEntity>(TEntity entity, DbContext context)
        where TEntity : class
    {
        PrepareAuditableEntity(entity, isNew: false, isDeleted: true);
        context.Set<TEntity>().Update(entity);

        await context.SaveChangesAsync();

        return Ok(true);
    }
}