namespace PortalSubastas.Reporting.Domain.Models;

public sealed record DetalleSubastaReport(
    DetalleSubastaCabecera Cabecera,
    IReadOnlyCollection<DetalleSubastaItem> Items,
    IReadOnlyCollection<DetalleSubastaProveedor> Proveedores);

public sealed record DetalleSubastaCabecera(
    int IdCotizacion,
    string NumeroCotizacion,
    string Titulo,
    string Estado,
    string TipoContratacion,
    string CriterioAdjudicacion,
    string UnidadAdministrativa,
    string? NumeroExpediente,
    decimal? MargenMejora,
    DateTime? FechaInicioSubasta,
    DateTime? FechaFinalizacionSubasta,
    DateTimeOffset FechaEmision);

public sealed record DetalleSubastaItem(
    int IdCotizacionDetalle,
    int? IdRenglon,
    int Numero,
    string Descripcion,
    decimal Cantidad,
    decimal ImporteBase,
    decimal TotalBase,
    decimal? ImporteMinimo,
    string? Moneda);

public sealed record DetalleSubastaProveedor(
    int IdProveedor,
    string Proveedor,
    string? Cuit,
    string EstadoParticipacion);
