namespace PortalSubastas.Reporting.Domain.Models;

public sealed record DesistimientoReport(
    DesistimientoCabecera Cabecera,
    string? Observaciones,
    string? UsuarioDesistimiento,
    DateTime? FechaDesistimiento);

public sealed record DesistimientoCabecera(
    int IdCotizacion,
    string NumeroCotizacion,
    string Titulo,
    string Estado,
    string TipoContratacion,
    string UnidadAdministrativa,
    string? NumeroExpediente,
    DateTimeOffset FechaEmision);
