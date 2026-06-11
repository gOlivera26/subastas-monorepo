namespace PortalSubastas.Contracts.Events;

public record ProveedorInvitadoEvent(
    int IdCotizacion,
    string NroCotizacion,
    string Titulo,
    int IdProveedor,
    string EmailProveedor,
    string NombreProveedor,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    string TipoContratacion,
    DateTime OccuredOn
);
