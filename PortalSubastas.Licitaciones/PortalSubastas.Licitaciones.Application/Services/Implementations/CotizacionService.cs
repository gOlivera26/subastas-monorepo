using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class CotizacionService : BaseService, ICotizacionService
{
    private readonly PortalSubastasContext _context;

    public CotizacionService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
    }

    public async Task<OperationResponse<List<CotizacionResponseDto>>> GetAllAsync(int? idVigencia)
    {
        var query = _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .AsQueryable();

        if (idVigencia.HasValue)
            query = query.Where(c => c.IdVigencia == idVigencia.Value);

        if (!IsSuperAdmin())
        {
            var orgId = GetUserOrganizationId();
            if (orgId.HasValue)
                query = query.Where(c => c.IdOrganizacion == orgId.Value);
        }

        var result = await query.OrderByDescending(c => c.IdCotizacion).ToListAsync();
        return Ok(_mapper.Map<List<CotizacionResponseDto>>(result));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> GetByIdAsync(int id)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> CreateAsync(CotizacionRequestDto dto)
    {
        var entity = _mapper.Map<TCotizacion>(dto);
        
        // Logica legacy de inicialización
        entity.IdEstado = 4; // Generado
        
        // Auto-numérico NRO_COTIZACION (simplificado para MVP, se debería hacer transaccional)
        var vigencia = await _context.TVigencias.FindAsync(dto.IdVigencia);
        var maxCotizacion = await _context.TCotizaciones
            .Where(c => c.IdVigencia == dto.IdVigencia && c.IdUnidadAdm == dto.IdUnidadAdm)
            .Select(c => c.NroCotizacion)
            .ToListAsync();
            
        int nextId = 1;
        if (maxCotizacion.Any())
        {
            var maxVal = maxCotizacion.Select(x => {
                var parts = x.Split('/');
                return parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 0;
            }).Max();
            nextId = maxVal + 1;
        }

        entity.NroCotizacion = $"{vigencia?.Ejercicio ?? DateTime.Now.Year}/{nextId.ToString().PadLeft(6, '0')}";

        PrepareAuditableEntity(entity, isNew: true);
        
        if (entity.Especificacion != null)
            PrepareAuditableEntity(entity.Especificacion, isNew: true);

        foreach(var det in entity.Detalles)
            PrepareAuditableEntity(det, isNew: true);

        _context.TCotizaciones.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> UpdateAsync(int id, CotizacionRequestDto dto)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        // Logica basica actualización
        _mapper.Map(dto, entity);
        PrepareAuditableEntity(entity, isNew: false);
        
        if (entity.Especificacion != null)
            PrepareAuditableEntity(entity.Especificacion, isNew: false);

        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var entity = await _context.TCotizaciones.FindAsync(id);
        if (entity == null) return NotFound<bool>();

        if (entity.IdEstado != 4)
            return BadRequest<bool>("Solo se puede anular una subasta en estado Generado.");

        entity.IdEstado = 20; // Anulado (baja lógica según legacy)
        PrepareAuditableEntity(entity, isNew: false, isDeleted: true);

        await _context.SaveChangesAsync();
        return Ok(true);
    }

    public async Task<OperationResponse<CotizacionResponseDto>> NotificarAsync(int id)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        if (entity.IdEstado != 4)
            return BadRequest<CotizacionResponseDto>("Solo se puede publicar una subasta en estado Generado.");

        entity.IdEstado = 39; // EnviadaPendiente (publicada)
        PrepareAuditableEntity(entity, isNew: false);
        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> FinalizarAsync(int id)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        if (entity.IdEstado != 39)
            return BadRequest<CotizacionResponseDto>("Solo se puede finalizar una subasta en estado EnviadaPendiente.");

        entity.IdEstado = 40; // Finalizada
        PrepareAuditableEntity(entity, isNew: false);

        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    // --- Endpoints Dashboard ---
    private IQueryable<TCotizacion> GetDashboardBaseQuery(int? idVigencia)
    {
        var query = _context.TCotizaciones
            .Include(c => c.Especificacion)
            .AsQueryable();

        if (idVigencia.HasValue)
            query = query.Where(c => c.IdVigencia == idVigencia.Value);

        return query;
    }

    public async Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasEnCursoAsync(int? idVigencia)
    {
        var now = DateTime.Now;
        var query = GetDashboardBaseQuery(idVigencia)
            .Where(c => c.IdEstado == 39 // EnvPend
                     && c.Especificacion.FechaInicioSubasta <= now 
                     && c.Especificacion.FechaFinalizacionSubasta >= now);

        var data = await query.ToListAsync();
        return Ok(MapToDashboard(data));
    }

    public async Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasProximasAsync(int? idVigencia)
    {
        var now = DateTime.Now;
        var query = GetDashboardBaseQuery(idVigencia)
            .Where(c => c.IdEstado == 39 // EnvPend
                     && c.Especificacion.FechaInicioSubasta > now)
            .OrderBy(c => c.Especificacion.FechaInicioSubasta)
            .Take(6);

        var data = await query.ToListAsync();
        return Ok(MapToDashboard(data));
    }

    public async Task<OperationResponse<List<SubastaDashboardDto>>> GetSubastasDelMesAsync(int? idVigencia)
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var query = GetDashboardBaseQuery(idVigencia)
            .Where(c => c.Especificacion.FechaInicioSubasta >= startOfMonth 
                     && c.Especificacion.FechaInicioSubasta <= endOfMonth)
            .OrderBy(c => c.Especificacion.FechaInicioSubasta);

        var data = await query.ToListAsync();
        return Ok(MapToDashboard(data));
    }

    private List<SubastaDashboardDto> MapToDashboard(List<TCotizacion> data)
    {
        return data.Select(c => new SubastaDashboardDto
        {
            IdCotizacion = c.IdCotizacion,
            NroCotizacion = c.NroCotizacion,
            Tipo = c.IdTipoContratacion == 7 ? "Subasta Inversa" : "Subasta Directa", // Simulado
            Estado = c.IdEstado == 39 ? "Publicada" : (c.IdEstado == 4 ? "Generado" : "Anulada"),
            Titulo = c.Observacion ?? "Subasta " + c.NroCotizacion,
            FechaInicio = c.Especificacion?.FechaInicioSubasta,
            FechaFin = c.Especificacion?.FechaFinalizacionSubasta
        }).ToList();
    }
}
