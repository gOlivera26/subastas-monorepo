using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;


namespace PortalSubastas.Providers.Domain.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPublishEndpoint _publishEndpoint;

    // Lista temporal para guardar los eventos hasta que la BD confirme los IDs
    private List<AuditEntryPending> _pendingAudits = new();

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor, IPublishEndpoint publishEndpoint)
    {
        _httpContextAccessor = httpContextAccessor;
        _publishEndpoint = publishEndpoint;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return new ValueTask<InterceptionResult<int>>(result);

        var username = GetCurrentUsername();
        var zoneAr = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zoneAr);

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdClaim, out var guid) ? guid : null;

        foreach (var entry in entries)
        {
            // ── 1. Llenar campos de auditoría y soft delete ──
            if (entry.Entity is IAuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.UsrIng = username;
                        auditable.FecIng = now;
                        break;

                    case EntityState.Modified:
                        if (entry.Entity is not IFullAuditableEntity { UsrBaja: not null })
                        {
                            auditable.UsrMod = username;
                            auditable.FecMod = now;
                        }
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        if (entry.Entity is IFullAuditableEntity fullAuditable)
                        {
                            fullAuditable.UsrBaja = username;
                            fullAuditable.FecBaja = now;
                        }
                        break;
                }
            }

            // ── 2. Capturar datos para DataChangedEvent ──
            var auditEntry = new AuditEntryPending
            {
                Entry = entry,
                TableName = entry.Metadata.GetTableName() ?? entry.Metadata.Name,
                Operation = entry.State.ToString().ToUpper(),
                UserId = userId
            };

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;

                if (entry.State == EntityState.Deleted)
                {
                    auditEntry.OldValues[propertyName] = property.OriginalValue!;
                }
                else if (entry.State == EntityState.Added)
                {
                    auditEntry.NewValues[propertyName] = property.CurrentValue!;
                    if (property.IsTemporary) auditEntry.TemporaryProperties.Add(property);
                }
                else if (entry.State == EntityState.Modified && property.IsModified)
                {
                    auditEntry.OldValues[propertyName] = property.OriginalValue!;
                    auditEntry.NewValues[propertyName] = property.CurrentValue!;
                }
            }

            if (auditEntry.OldValues.Count > 0 || auditEntry.NewValues.Count > 0)
            {
                _pendingAudits.Add(auditEntry);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        foreach (var audit in _pendingAudits)
        {
            foreach (var prop in audit.TemporaryProperties)
            {
                audit.NewValues[prop.Metadata.Name] = prop.CurrentValue!;
            }

            var primaryKey = audit.Entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "0";

            var auditEvent = new DataChangedEvent(
                UserId: audit.UserId,
                TableName: audit.TableName,
                RecordId: primaryKey,
                OperationType: audit.Operation,
                OldValues: audit.OldValues.Count > 0 ? JsonSerializer.Serialize(audit.OldValues) : null,
                NewValues: audit.NewValues.Count > 0 ? JsonSerializer.Serialize(audit.NewValues) : null,
                OccurredAt: TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"))
            );

            await _publishEndpoint.Publish(auditEvent, cancellationToken);
        }

        _pendingAudits.Clear();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private string GetCurrentUsername()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Sistema";
    }
}

public class AuditEntryPending
{
    public EntityEntry Entry { get; set; } = null!;
    public string TableName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Dictionary<string, object> OldValues { get; } = new();
    public Dictionary<string, object> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();
}
