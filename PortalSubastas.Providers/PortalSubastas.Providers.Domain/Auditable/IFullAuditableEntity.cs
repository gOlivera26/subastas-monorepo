namespace PortalSubastas.Providers.Domain.Auditable;

public interface IFullAuditableEntity : IAuditableEntity
{
    string UsrBaja { get; set; }
    DateTime? FecBaja { get; set; }
}
