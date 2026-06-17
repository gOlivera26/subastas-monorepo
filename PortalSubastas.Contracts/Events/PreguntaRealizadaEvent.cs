namespace PortalSubastas.Contracts.Events;

public record PreguntaRealizadaEvent(
    int IdCotizacion,
    string NroCotizacion,
    string Titulo,
    string ContenidoPregunta,
    string EmailOrganismo,
    string NombreOrganismo,
    string UsuarioProveedor,
    DateTime OccuredOn
);
