namespace PortalSubastas.Reporting.Domain.Models;

public sealed record ProveedoresInvitadosReport(
    ProveedoresInvitadosCabecera Cabecera,
    IReadOnlyCollection<ProveedorInvitado> Proveedores);

public sealed record ProveedoresInvitadosCabecera(
    int IdCotizacion,
    string NumeroCotizacion,
    string Titulo,
    string Estado,
    string TipoContratacion,
    string UnidadAdministrativa,
    string? NumeroExpediente,
    DateTimeOffset FechaEmision);

public sealed record ProveedorInvitado(
    int IdProveedor,
    string RazonSocial,
    string? Cuit,
    string? Mail,
    string? Telefono);
