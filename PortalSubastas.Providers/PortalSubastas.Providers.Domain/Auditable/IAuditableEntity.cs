namespace PortalSubastas.Providers.Domain.Auditable;

public interface IAuditableEntity
{
    string UsrIng { get; set; }
    DateTime? FecIng { get; set; }
    string UsrMod { get; set; }
    DateTime? FecMod { get; set; }
}
