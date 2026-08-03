namespace PortalSubastas.Reporting.Domain.Models;

public sealed record VerificacionDocumentacionReport(
    VerificacionDocumentacionCabecera Cabecera,
    IReadOnlyCollection<VerificacionDocumentacionItem> Documentos);

public sealed record VerificacionDocumentacionCabecera(
    int IdCotizacion,
    string NumeroCotizacion,
    string Titulo,
    string Estado,
    string TipoContratacion,
    string CriterioAdjudicacion,
    string UnidadAdministrativa,
    string? NumeroExpediente,
    DateTime? FechaInicioSubasta,
    DateTime? FechaFinalizacionSubasta,
    DateTimeOffset FechaEmision,
    string NotaAdecuacion);

public sealed record VerificacionDocumentacionItem(
    string Origen,
    int? IdProveedor,
    string Proveedor,
    string? Cuit,
    string TipoDocumento,
    string? NombreArchivo,
    string? UrlArchivo,
    string Estado,
    DateTime? FechaPresentacion,
    string? Observaciones);
