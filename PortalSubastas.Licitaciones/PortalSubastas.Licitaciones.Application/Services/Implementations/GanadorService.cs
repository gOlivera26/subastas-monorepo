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

    public async Task<OperationResponse<GanadorResponseDto>> CreateAsync(GanadorRequestDto dto)
    {
        var entity = _mapper.Map<TGanador>(dto);
        PrepareAuditableEntity(entity, isNew: true);
        _context.TGanadores.Add(entity);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "GANADOR_REGISTRADO", "LICITACIONES", new { entity.IdGanador, entity.IdCotizacion, entity.MontoGanador });

        // 3. Publicar GanadorRegistradoEvent para email a los representantes del ganador
        try
        {
            var cotizacion = await _context.TCotizaciones
                .Include(c => c.Especificacion)
                .FirstOrDefaultAsync(c => c.IdCotizacion == dto.IdCotizacion);

            var tipoNombre = (cotizacion?.IdTipoContratacion ?? 0).ToDisplayName();

            var representantes = await GetRepresentantesAsync(dto.IdProveedor);

            foreach (var (email, nombre) in representantes)
            {
                await _publishEndpoint.Publish(new GanadorRegistradoEvent(
                    IdCotizacion: dto.IdCotizacion,
                    NroCotizacion: cotizacion?.NroCotizacion ?? "",
                    Titulo: cotizacion?.Observacion ?? "Subasta",
                    IdProveedor: dto.IdProveedor,
                    EmailProveedor: email,
                    NombreProveedor: nombre,
                    RazonSocialProveedor: cotizacion?.IdTipoContratacion.ToString() ?? "",
                    CuitProveedor: "",
                    MontoGanador: dto.MontoGanador,
                    TipoContratacion: tipoNombre,
                    OccuredOn: DateTime.UtcNow
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ No se pudo publicar GanadorRegistradoEvent para Cotización {IdCotizacion}", dto.IdCotizacion);
        }

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
}
