using PortalSubastas.Identity.Domain.Auditable;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TCatalogoBien : IFullAuditableEntity
{
    public int IdItem { get; set; }
    public int? IdItemRel { get; set; }
    public string Codigo { get; set; }
    public string NItem { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? IdObjetoGasto { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TCatalogoBien IdItemRelNavigation { get; set; }
    public virtual ICollection<TCatalogoBien> InverseIdItemRelNavigation { get; set; } = new List<TCatalogoBien>();
    public virtual TVigencia IdVigenciaNavigation { get; set; }
    public virtual TOrganizacione IdOrganizacionNavigation { get; set; }
    public virtual TObjetoGasto IdObjetoGastoNavigation { get; set; }
}
