#nullable disable
using System;
using System.Collections.Generic;

namespace PortalSubastas.Providers.Domain.Models;

public partial class TPersona : IFullAuditableEntity
{
    public int Id { get; set; }
    public int IdTipoPersona { get; set; }
    public int IdTipoDocumento { get; set; }
    public string NroDocumento { get; set; }
    public string Telefono { get; set; }
    public string EmailContacto { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }

    public virtual TTiposPersona IdTipoPersonaNavigation { get; set; }
    public virtual ICollection<TDomicilio> TDomicilios { get; set; } = new List<TDomicilio>();
}
