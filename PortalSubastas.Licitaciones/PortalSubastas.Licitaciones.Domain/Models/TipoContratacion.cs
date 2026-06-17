namespace PortalSubastas.Licitaciones.Domain.Models;

/// <summary>
/// Tipos fijos de contratación para subastas.
/// Mapea a los valores de TCotizacion.IdTipoContratacion.
/// </summary>
public enum TipoContratacion
{
    SubastaInversa = 7,
    SubastaDirecta = 9,
    SubastaInversaMontoFijo = 13,
    SubastaInversaSEEC = 15
}
