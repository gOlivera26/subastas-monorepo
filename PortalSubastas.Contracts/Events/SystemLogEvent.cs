namespace PortalSubastas.Contracts.Events;

public record SystemLogEvent(
    Guid? UserId,
    string Username,
    string Action,
    string Module,
    string Details,
    string IpAddress,
    DateTime OccurredAt
);