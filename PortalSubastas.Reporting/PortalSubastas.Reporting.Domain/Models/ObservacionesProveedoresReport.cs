namespace PortalSubastas.Reporting.Domain.Models;

public sealed record ObservacionesProveedoresReport(
    ObservacionesProveedoresCabecera Cabecera,
    IReadOnlyCollection<ObservacionProveedor> Observaciones);

public sealed record ObservacionesProveedoresCabecera(
    int IdCotizacion,
    string NumeroCotizacion,
    string Titulo,
    string Estado,
    string TipoContratacion,
    string UnidadAdministrativa,
    string? NumeroExpediente,
    DateTime? FechaLimiteImpugnar,
    DateTimeOffset FechaEmision);

public sealed record ObservacionProveedor(
    int? IdProveedor,
    string Proveedor,
    string? Cuit,
    string Observacion,
    string Origen);
