using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Licitaciones.Application.RequestDto.Ganador;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Ganador;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class GanadorService : BaseService, IGanadorService
{
    private const int EstadoFinalizada = 40;
    private const int TipoContratacionDirecta = 9;
    private const int CriterioRenglon = 1;

    private readonly PortalSubastasContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<GanadorService> _logger;
    private readonly IProveedorRepresentanteService _proveedorRepresentanteService;

    public GanadorService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache, IPublishEndpoint publishEndpoint, ILogger<GanadorService> logger, IProveedorRepresentanteService proveedorRepresentanteService)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _proveedorRepresentanteService = proveedorRepresentanteService;
    }

    public async Task<OperationResponse<List<GanadorResponseDto>>> GetAllAsync(int idCotizacion)
    {
        var query = _context.TGanadores
            .Include(g => g.IdCotizacionNavigation)
            .Where(g => g.IdCotizacion == idCotizacion && g.FecBaja == null);

        var result = await query.OrderBy(g => g.MontoGanador).ToListAsync();
        return Ok(_mapper.Map<List<GanadorResponseDto>>(result));
    }

    public async Task<OperationResponse<List<GanadorResponseDto>>> GenerateAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .Include(c => c.Renglones)
            .Include(c => c.Ofertas)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion && c.FecBaja == null);

        if (cotizacion is null)
            return NotFound<List<GanadorResponseDto>>();

        if (cotizacion.Especificacion?.FechaFinalizacionSubasta is DateTime fechaFin && fechaFin > DateTime.Now)
            return BadRequest<List<GanadorResponseDto>>("No se pueden generar ganadores antes de la finalización de la subasta.");

        if (cotizacion.IdEstado != EstadoFinalizada)
            return BadRequest<List<GanadorResponseDto>>("Solo se pueden generar ganadores para una subasta finalizada.");

        var existentes = await _context.TGanadores
            .Where(g => g.IdCotizacion == idCotizacion && g.FecBaja == null)
            .ToListAsync();

        var nuevos = cotizacion.Especificacion?.CriterioAdjudicacion == CriterioRenglon
            ? BuildGanadoresPorRenglon(cotizacion, existentes)
            : BuildGanadoresPorItem(cotizacion, existentes);

        if (nuevos.Count > 0)
        {
            foreach (var ganador in nuevos)
                PrepareAuditableEntity(ganador, isNew: true);

            _context.TGanadores.AddRange(nuevos);
            await _context.SaveChangesAsync();

            await PublishSystemLogAsync(_publishEndpoint, "GANADORES_GENERADOS", "LICITACIONES", new
            {
                cotizacion.IdCotizacion,
                cotizacion.NroCotizacion,
                Cantidad = nuevos.Count
            });

            foreach (var ganador in nuevos)
                await TryPublishGanadorRegistradoAsync(cotizacion, ganador.IdProveedor, ganador.MontoGanador);
        }

        var result = await _context.TGanadores
            .Include(g => g.IdCotizacionNavigation)
            .Where(g => g.IdCotizacion == idCotizacion && g.FecBaja == null)
            .OrderBy(g => g.IdRenglon)
            .ThenBy(g => g.IdCotizacionDetalle)
            .ThenBy(g => g.MontoGanador)
            .ToListAsync();

        return Ok(_mapper.Map<List<GanadorResponseDto>>(result));
    }

    public async Task<OperationResponse<GanadorResponseDto>> CreateAsync(GanadorRequestDto dto)
    {
        var entity = _mapper.Map<TGanador>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.TGanadores.Add(entity);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "GANADOR_REGISTRADO", "LICITACIONES", new { entity.IdGanador, entity.IdCotizacion, entity.MontoGanador });

        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == dto.IdCotizacion);

        if (cotizacion is not null)
            await TryPublishGanadorRegistradoAsync(cotizacion, dto.IdProveedor, dto.MontoGanador);

        return Ok(_mapper.Map<GanadorResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TGanadores.FindAsync(id);
        if (entity == null) return NotFound<bool>();
        var result = await DeleteAsync(entity, _context);
        if (result.Success == true)
            await PublishSystemLogAsync(_publishEndpoint, "GANADOR_ELIMINADO", "LICITACIONES", new { entity.IdGanador, entity.IdCotizacion });
        return result;
    }

    private async Task<List<(string Email, string NombrePersona)>> GetRepresentantesAsync(int idProveedor)
        => await _proveedorRepresentanteService.GetRepresentantesAsync(idProveedor);

    private static List<TGanador> BuildGanadoresPorItem(TCotizacion cotizacion, IReadOnlyCollection<TGanador> existentes)
    {
        var ganadores = new List<TGanador>();
        var directa = cotizacion.IdTipoContratacion == TipoContratacionDirecta;

        foreach (var detalle in cotizacion.Detalles.Where(d => d.FecBaja == null).OrderBy(d => d.IdCotizacionDetalle))
        {
            if (existentes.Any(g => g.IdCotizacionDetalle == detalle.IdCotizacionDetalle))
                continue;

            var oferta = GetMejorOferta(
                cotizacion.Ofertas.Where(o => o.FecBaja == null && o.IdCotizacionDetalle == detalle.IdCotizacionDetalle),
                directa);

            if (oferta is null)
                continue;

            ganadores.Add(new TGanador
            {
                IdCotizacion = cotizacion.IdCotizacion,
                IdCotizacionDetalle = detalle.IdCotizacionDetalle,
                IdRenglon = null,
                IdProveedor = oferta.IdProveedor,
                MontoGanador = oferta.Monto,
                CantidadAdjudicada = detalle.Cantidad
            });
        }

        return ganadores;
    }

    private static List<TGanador> BuildGanadoresPorRenglon(TCotizacion cotizacion, IReadOnlyCollection<TGanador> existentes)
    {
        var ganadores = new List<TGanador>();
        var directa = cotizacion.IdTipoContratacion == TipoContratacionDirecta;

        foreach (var renglon in cotizacion.Renglones.Where(r => r.FecBaja == null).OrderBy(r => r.NumeroRenglon))
        {
            if (existentes.Any(g => g.IdRenglon == renglon.IdRenglon))
                continue;

            var oferta = GetMejorOferta(
                cotizacion.Ofertas.Where(o => o.FecBaja == null && o.IdRenglon == renglon.IdRenglon),
                directa);

            if (oferta is null)
                continue;

            var cantidad = cotizacion.Detalles
                .Where(d => d.FecBaja == null && d.IdRenglon == renglon.IdRenglon)
                .Sum(d => d.Cantidad);

            ganadores.Add(new TGanador
            {
                IdCotizacion = cotizacion.IdCotizacion,
                IdCotizacionDetalle = null,
                IdRenglon = renglon.IdRenglon,
                IdProveedor = oferta.IdProveedor,
                MontoGanador = oferta.Monto,
                CantidadAdjudicada = cantidad
            });
        }

        return ganadores;
    }

    private static TOfertaSubasta? GetMejorOferta(IEnumerable<TOfertaSubasta> ofertas, bool directa)
    {
        return directa
            ? ofertas.OrderByDescending(o => o.Monto).ThenBy(o => o.FechaOferta).ThenBy(o => o.IdOfertaSubasta).FirstOrDefault()
            : ofertas.OrderBy(o => o.Monto).ThenBy(o => o.FechaOferta).ThenBy(o => o.IdOfertaSubasta).FirstOrDefault();
    }

    private async Task TryPublishGanadorRegistradoAsync(TCotizacion cotizacion, int idProveedor, decimal montoGanador)
    {
        try
        {
            var tipoNombre = cotizacion.IdTipoContratacion.ToDisplayName();
            var representantes = await GetRepresentantesAsync(idProveedor);

            foreach (var (email, nombre) in representantes)
            {
                await _publishEndpoint.Publish(new GanadorRegistradoEvent(
                    IdCotizacion: cotizacion.IdCotizacion,
                    NroCotizacion: cotizacion.NroCotizacion,
                    Titulo: cotizacion.Observacion ?? "Subasta",
                    IdProveedor: idProveedor,
                    EmailProveedor: email,
                    NombreProveedor: nombre,
                    RazonSocialProveedor: "",
                    CuitProveedor: "",
                    MontoGanador: montoGanador,
                    TipoContratacion: tipoNombre,
                    OccuredOn: DateTime.UtcNow
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo publicar GanadorRegistradoEvent para Cotización {IdCotizacion}", cotizacion.IdCotizacion);
        }
    }
}
