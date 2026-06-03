using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion.Consultas;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class ConsultaService : BaseService, IConsultaService
{
    private new readonly PortalSubastasContext _context;
    private readonly ISubastaNotificationService _notificationService;

    public ConsultaService(
        PortalSubastasContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        ISubastaNotificationService notificationService)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<OperationResponse<List<ConsultaResponseDto>>> GetConsultasAsync(int idCotizacion)
    {
        var mensajes = await _context.TMensajes
            .Where(m => m.IdCotizacion == idCotizacion)
            .OrderBy(m => m.FecIng)
            .ToListAsync();

        return Ok(_mapper.Map<List<ConsultaResponseDto>>(mensajes));
    }

    public async Task<OperationResponse<ConsultaResponseDto>> RealizarPreguntaAsync(int idCotizacion, PreguntaRequestDto dto)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion == null) return NotFound<ConsultaResponseDto>();

        // 1. Validar Límite de Fechas (Regla de negocio del Legacy)
        if (cotizacion.Especificacion?.FechaLimiteConsultas < DateTime.Now)
            return BadRequest<ConsultaResponseDto>("Se venció el plazo límite para realizar preguntas.");

        var idProveedor = GetUserProveedorId();

        // 2. Guardar en Base de Datos
        var entity = new TMensaje
        {
            IdCotizacion = idCotizacion,
            IdProveedor = idProveedor,
            Usuario = GetCurrentUsername(),
            Contenido = dto.Contenido,
            FecIng = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        _context.TMensajes.Add(entity);
        await _context.SaveChangesAsync();

        var resultDto = _mapper.Map<ConsultaResponseDto>(entity);

        // 3. Notificar por SignalR a todos en la sala "chat_{id}"
        await _notificationService.NotificarNuevaPreguntaAsync(idCotizacion, resultDto);

        // TODO: FASE 6 - Integrar envío de email al Organismo alertando la nueva pregunta.
        // await PublishSystemLogAsync(_publishEndpoint, "NUEVA_PREGUNTA_EMAIL", ...);

        return Ok(resultDto);
    }

    public async Task<OperationResponse<ConsultaResponseDto>> ResponderPreguntaAsync(int idCotizacion, int idMensaje, RespuestaRequestDto dto)
    {
        if (!IsSuperAdmin())
            return Unauthorized<ConsultaResponseDto>("Solo los administradores pueden responder consultas.");

        var mensaje = await _context.TMensajes.FirstOrDefaultAsync(m => m.IdMensaje == idMensaje && m.IdCotizacion == idCotizacion);
        if (mensaje == null) return NotFound<ConsultaResponseDto>();

        // Llenar campos de respuesta (Nuevos campos BD)
        mensaje.Respuesta = dto.Respuesta;
        mensaje.FechaRespuesta = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        mensaje.UsuarioRespuesta = GetCurrentUsername();

        await _context.SaveChangesAsync();

        var resultDto = _mapper.Map<ConsultaResponseDto>(mensaje);

        // Notificar respuesta en vivo
        await _notificationService.NotificarNuevaRespuestaAsync(idCotizacion, resultDto);

        return Ok(resultDto);
    }
}
