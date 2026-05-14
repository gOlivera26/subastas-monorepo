namespace PortalSubastas.Contracts.Events;

public record DataChangedEvent(
 Guid? UserId,
 string TableName,
 string RecordId,
 string OperationType, // INSERT, UPDATE, DELETE
 string? OldValues,    // JSON
 string? NewValues,    // JSON
 DateTime OccurredAt
);
