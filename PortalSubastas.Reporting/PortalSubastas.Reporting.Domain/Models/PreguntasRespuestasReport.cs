namespace PortalSubastas.Reporting.Domain.Models;

public sealed record PreguntasRespuestasReport(
    PreguntasRespuestasCabecera Cabecera,
    IReadOnlyCollection<PreguntaRespuesta> Consultas);

public sealed record PreguntasRespuestasCabecera(
    int IdCotizacion,
    string NumeroCotizacion,
    string Titulo,
    string Estado,
    string TipoContratacion,
    string UnidadAdministrativa,
    string? NumeroExpediente,
    DateTimeOffset FechaEmision);

public sealed record PreguntaRespuesta(
    int IdMensaje,
    int? IdProveedor,
    string UsuarioPregunta,
    string Pregunta,
    DateTime FechaPregunta,
    string? Respuesta,
    string? UsuarioRespuesta,
    DateTime? FechaRespuesta);
