namespace PortalSubastas.Contracts.Events;

public record PreguntaRespondidaEvent(
    int IdCotizacion,
    string NroCotizacion,
    string Titulo,
    string ContenidoPregunta,
    string ContenidoRespuesta,
    string EmailProveedor,
    string NombreProveedor,
    DateTime OccuredOn
);
