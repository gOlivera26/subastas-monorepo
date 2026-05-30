#nullable disable
using PortalSubastas.Identity.Domain.Auditable;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TMoneda
{
    public int IdMoneda { get; set; }
    public string Simbolo { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public DateTime? FecBaja { get; set; }
}
