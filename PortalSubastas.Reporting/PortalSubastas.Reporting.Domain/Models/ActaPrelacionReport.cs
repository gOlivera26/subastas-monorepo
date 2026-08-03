namespace PortalSubastas.Reporting.Domain.Models;

public sealed record ActaPrelacionReport(
    ActaPrelacionCabecera Cabecera,
    IReadOnlyCollection<ActaPrelacionDetalle> Detalles,
    IReadOnlyCollection<ActaPrelacionOferta> OfertasIniciales,
    IReadOnlyCollection<ActaPrelacionOferta> HistorialOfertas,
    IReadOnlyCollection<ActaPrelacionGanador> Ganadores);

public sealed record ActaPrelacionCabecera(
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

public sealed record ActaPrelacionDetalle(
    int IdCotizacionDetalle,
    int? IdRenglon,
    int Numero,
    string Descripcion,
    decimal Cantidad,
    decimal PrecioBase,
    decimal TotalBase);

public sealed record ActaPrelacionOferta(
    int IdOfertaSubasta,
    int IdProveedor,
    string Proveedor,
    string? Cuit,
    int? IdCotizacionDetalle,
    int? IdRenglon,
    int NumeroDetalle,
    string Detalle,
    decimal Cantidad,
    decimal Monto,
    decimal Total,
    DateTime FechaOferta,
    int NumeroOferta,
    bool EsOfertaInicial);

public sealed record ActaPrelacionGanador(
    int IdProveedor,
    string Proveedor,
    string? Cuit,
    int? IdCotizacionDetalle,
    int? IdRenglon,
    int NumeroDetalle,
    string Detalle,
    decimal MontoGanador,
    decimal CantidadAdjudicada);
