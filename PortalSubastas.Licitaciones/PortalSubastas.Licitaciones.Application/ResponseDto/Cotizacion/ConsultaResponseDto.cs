namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

public class ConsultaResponseDto
{
    public int IdMensaje { get; set; }
    public int IdCotizacion { get; set; }
    public int? IdProveedor { get; set; }
    public string UsuarioPregunta { get; set; } = null!;
    public string Pregunta { get; set; } = null!;
    public DateTime FechaPregunta { get; set; }

    public string? Respuesta { get; set; }
    public string? UsuarioRespuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public bool Respondida => !string.IsNullOrEmpty(Respuesta);
}
