namespace PortalSubastas.Contracts.Events;

public record UserApprovedEvent(
    Guid UserId,
    string AdminName,
    DateTime ApprovedAt,
    string Action = "USUARIO_APROBADO",
    string Module = "IAM",
    string? IpAddress = null
);
