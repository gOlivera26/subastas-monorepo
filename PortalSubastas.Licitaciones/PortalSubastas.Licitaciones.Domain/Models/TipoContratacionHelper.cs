namespace PortalSubastas.Licitaciones.Domain.Models;

/// <summary>
/// Helper para convertir IdTipoContratacion (int) a nombres descriptivos.
/// Centraliza los switch hardcodeados que estaban duplicados en services.
/// </summary>
public static class TipoContratacionHelper
{
    public static string ToDisplayName(this int idTipo) => idTipo switch
    {
        (int)TipoContratacion.SubastaInversa => "Subasta Inversa",
        (int)TipoContratacion.SubastaDirecta => "Subasta Directa",
        (int)TipoContratacion.SubastaInversaMontoFijo => "Subasta Inversa Monto Fijo",
        (int)TipoContratacion.SubastaInversaSEEC => "Subasta Inversa SEEC",
        _ => "Subasta"
    };

    /// <summary>
    /// Solo se usa en el Dashboard. El resto del sistema usa la versión corta.
    /// </summary>
    public static string ToFullDisplayName(this int idTipo) => idTipo switch
    {
        (int)TipoContratacion.SubastaInversa => "Subasta Electrónica Inversa",
        (int)TipoContratacion.SubastaDirecta => "Subasta Electrónica Directa",
        (int)TipoContratacion.SubastaInversaMontoFijo => "Subasta Inversa Monto Fijo",
        (int)TipoContratacion.SubastaInversaSEEC => "Subasta Inversa SEEC",
        _ => "Subasta"
    };
}
