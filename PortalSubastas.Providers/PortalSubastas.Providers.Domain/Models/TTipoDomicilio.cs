#nullable disable
using System;
using System.Collections.Generic;

namespace PortalSubastas.Providers.Domain.Models;

public partial class TTipoDomicilio : IFullAuditableEntity
{
    public int Id { get; set; }
    public string Descripcion { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual ICollection<TDomicilio> TDomicilios { get; set; } = new List<TDomicilio>();
}
