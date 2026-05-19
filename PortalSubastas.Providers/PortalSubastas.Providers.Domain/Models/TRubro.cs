#nullable disable
using System;
using System.Collections.Generic;

namespace PortalSubastas.Providers.Domain.Models;

public partial class TRubro : IFullAuditableEntity
{
    public int Id { get; set; }
    public string Codigo { get; set; }
    public string Descripcion { get; set; }
    public int? IdRubroPadre { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("imputable")]
    public bool Imputable { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TRubro IdRubroPadreNavigation { get; set; }
    public virtual ICollection<TRubro> Hijos { get; set; } = new List<TRubro>();
    public virtual ICollection<TProveedoresRubro> TProveedoresRubros { get; set; } = new List<TProveedoresRubro>();
}
