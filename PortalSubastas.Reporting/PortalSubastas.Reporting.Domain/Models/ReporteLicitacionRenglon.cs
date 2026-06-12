namespace PortalSubastas.Reporting.Domain.Models;

public sealed record ReporteLicitacionRenglon(
    int Numero,
    string Descripcion,
    decimal Cantidad,
    string UnidadMedida,
    decimal PrecioEstimado);
