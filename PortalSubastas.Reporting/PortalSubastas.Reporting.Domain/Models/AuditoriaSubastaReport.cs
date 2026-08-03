namespace PortalSubastas.Reporting.Domain.Models;

public sealed record AuditoriaSubastaReport(
    AuditoriaSubastaCabecera Cabecera,
    AuditoriaSubastaResumen Resumen,
    IReadOnlyCollection<AuditoriaSubastaMovimiento> Movimientos);

public sealed record AuditoriaSubastaCabecera(
    int IdCotizacion,
    string NumeroCotizacion,
    string Titulo,
    string Estado,
    string TipoContratacion,
    string CriterioAdjudicacion,
    string UnidadAdministrativa,
    string? NumeroExpediente,
    DateTime? FechaCotizacion,
    DateTime? FechaInicioSubasta,
    DateTimeOffset FechaEmision);

public sealed record AuditoriaSubastaResumen(
    int CantidadDocumentos,
    int CantidadOfertas,
    int CantidadProveedores);

public sealed record AuditoriaSubastaMovimiento(
    string Tipo,
    string Descripcion,
    int Cantidad,
    string? Usuario,
    DateTime? Fecha);
