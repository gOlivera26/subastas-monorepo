namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class PreguntasRespuestasReportResponseDto
{
    public PreguntasRespuestasCabeceraResponseDto Cabecera { get; set; } = new();
    public List<PreguntaRespuestaResponseDto> Consultas { get; set; } = new();
}

public sealed class PreguntasRespuestasCabeceraResponseDto
{
    public int IdCotizacion { get; set; }
    public string NumeroCotizacion { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoContratacion { get; set; } = string.Empty;
    public string UnidadAdministrativa { get; set; } = string.Empty;
    public string? NumeroExpediente { get; set; }
    public DateTimeOffset FechaEmision { get; set; }
}

public sealed class PreguntaRespuestaResponseDto
{
    public int IdMensaje { get; set; }
    public int? IdProveedor { get; set; }
    public string UsuarioPregunta { get; set; } = string.Empty;
    public string Pregunta { get; set; } = string.Empty;
    public DateTime FechaPregunta { get; set; }
    public string? Respuesta { get; set; }
    public string? UsuarioRespuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
}
