#nullable disable
using PortalSubastas.Identity.Domain.Auditable;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TUnidadAdministrativa : IFullAuditableEntity
{
    public int IdUnidadAdm { get; set; }
    public int NumeroUnidadAdm { get; set; }
    public string NombreUnidadAdm { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public string Mail { get; set; }
    public string Alias { get; set; }
    public short? Puerto { get; set; }
    public string Smtp { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TVigencia IdVigenciaNavigation { get; set; }
    public virtual TOrganizacione IdOrganizacionNavigation { get; set; }
}
