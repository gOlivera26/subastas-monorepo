#nullable disable
using System;
using System.Collections.Generic;

namespace PortalSubastas.Providers.Domain.Models;

public partial class TTiposPersona : IFullAuditableEntity
{
    public int Id { get; set; }
    public string Descripcion { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual ICollection<TPersona> TPersonas { get; set; } = new List<TPersona>();
    public virtual ICollection<TProveedore> TProveedores { get; set; } = new List<TProveedore>();
}
