namespace PortalSubastas.Reporting.Domain.Models;

public sealed record ReporteLicitacion(
    int IdCotizacion,
    string Numero,
    string Titulo,
    string Estado,
    DateTimeOffset FechaEmision,
    IReadOnlyCollection<ReporteLicitacionRenglon> Renglones);
