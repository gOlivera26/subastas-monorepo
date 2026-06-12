namespace PortalSubastas.Contracts.Events;

public record GanadorRegistradoEvent(
    int IdCotizacion,
    string NroCotizacion,
    string Titulo,
    int IdProveedor,
    string EmailProveedor,
    string NombreProveedor,
    string RazonSocialProveedor,
    string CuitProveedor,
    decimal MontoGanador,
    string TipoContratacion,
    DateTime OccuredOn
);
