using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion.Consultas;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IConsultaService
{
    Task<OperationResponse<List<ConsultaResponseDto>>> GetConsultasAsync(int idCotizacion);
    Task<OperationResponse<ConsultaResponseDto>> RealizarPreguntaAsync(int idCotizacion, PreguntaRequestDto dto);
    Task<OperationResponse<ConsultaResponseDto>> ResponderPreguntaAsync(int idCotizacion, int idMensaje, RespuestaRequestDto dto);
}
