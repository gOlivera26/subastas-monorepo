#nullable disable
using System;
using System.Collections.Generic;

namespace PortalSubastas.Identity.Domain.Models;

public partial class TDomicilio : IFullAuditableEntity
{
    public int Id { get; set; }
    public int IdPersona { get; set; }
    public int IdTipoDomicilio { get; set; }
    public string Calle { get; set; }
    public string Numero { get; set; }
    public string Piso { get; set; }
    public string Departamento { get; set; }
    public string Barrio { get; set; }
    public string Ciudad { get; set; }
    public int IdProvincia { get; set; }
    public string CodigoPostal { get; set; }
    public string Telefono { get; set; }
    public string Fax { get; set; }
    public string UsrIng { get; set; }
    public DateTime? FecIng { get; set; }
    public string UsrMod { get; set; }
    public DateTime? FecMod { get; set; }
    public string UsrBaja { get; set; }
    public DateTime? FecBaja { get; set; }

    public virtual TPersona IdPersonaNavigation { get; set; }
    public virtual TTipoDomicilio IdTipoDomicilioNavigation { get; set; }
    public virtual TProvincia IdProvinciaNavigation { get; set; }
}
