using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace PortalSubastas.Identity.Domain.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Lista temporal para guardar los eventos hasta que la BD confirme los IDs
    private List<AuditEntryPending> _pendingAudits = new();

    public AuditInterceptor(IPublishEndpoint publishEndpoint, IHttpContextAccessor httpContextAccessor)
    {
        _publishEndpoint = publishEndpoint;
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return new ValueTask<InterceptionResult<int>>(result);

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdClaim, out var guid) ? guid : null;

        foreach (var entry in entries)
        {
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
                    // Si el ID es autoincremental, lo marcamos para actualizarlo después
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

            var primaryKey = audit.Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "0";

            var auditEvent = new DataChangedEvent(
                UserId: audit.UserId,
                TableName: audit.TableName,
                RecordId: primaryKey,
                OperationType: audit.Operation,
                OldValues: audit.OldValues.Count > 0 ? JsonSerializer.Serialize(audit.OldValues) : null,
                NewValues: audit.NewValues.Count > 0 ? JsonSerializer.Serialize(audit.NewValues) : null,
                OccurredAt: DateTime.UtcNow
            );

            await _publishEndpoint.Publish(auditEvent, cancellationToken);
        }

        _pendingAudits.Clear();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
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