#nullable disable
using System;
using System.Collections.Generic;

namespace PortalSubastas.Providers.Domain.Models;

public partial class TProveedoresRubro : IFullAuditableEntity
{
    public int IdProveedor { get; set; }
    public int IdRubro { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TProveedore IdProveedorNavigation { get; set; }
    public virtual TRubro IdRubroNavigation { get; set; }
}
