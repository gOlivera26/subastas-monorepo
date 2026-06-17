namespace PortalSubastas.Reporting.Domain.Models;

public sealed record ReporteMetadata(
    TipoReporte Tipo,
    string Title,
    DateTimeOffset GeneratedAt);
