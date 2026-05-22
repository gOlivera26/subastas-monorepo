using PortalSubastas.Identity.Domain.Auditable;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TObjetoGasto : IFullAuditableEntity
{
    public int IdObjetoGasto { get; set; }
    public int? IdObjetoGastoRel { get; set; }
    public string NumeroObjeto { get; set; }
    public string NombreObjeto { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public bool? ImputaEjecucion { get; set; }
    public string? UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string? UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string? UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TObjetoGasto IdObjetoGastoRelNavigation { get; set; }
    public virtual ICollection<TObjetoGasto> InverseIdObjetoGastoRelNavigation { get; set; } = new List<TObjetoGasto>();
    public virtual TVigencia IdVigenciaNavigation { get; set; }
    public virtual TOrganizacione IdOrganizacionNavigation { get; set; }
    public virtual ICollection<TCatalogoBien> TCatalogosBien { get; set; } = new List<TCatalogoBien>();
}
