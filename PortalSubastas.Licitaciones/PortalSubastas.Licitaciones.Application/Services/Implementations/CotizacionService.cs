using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortalSubastas.Contracts.Events;
using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Common;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion.Publica;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class CotizacionService : BaseService, ICotizacionService
{
    private readonly PortalSubastasContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CotizacionService> _logger;
    private readonly IProveedorRepresentanteService _proveedorRepresentanteService;

    public CotizacionService(PortalSubastasContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IMemoryCache cache, IPublishEndpoint publishEndpoint, ILogger<CotizacionService> logger, IProveedorRepresentanteService proveedorRepresentanteService)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _proveedorRepresentanteService = proveedorRepresentanteService;
    }

    public async Task<OperationResponse<List<CotizacionResponseDto>>> GetAllAsync(int? idVigencia)
    {
        var query = _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
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
            .Include(c => c.IdUnidadAdmNavigation)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        var dto = _mapper.Map<CotizacionResponseDto>(entity);

        dto.Tipo = entity.IdTipoContratacion.ToDisplayName();
        dto.Estado = GetEstadoNombre(entity.IdEstado);
        dto.Modalidad = entity.Especificacion?.Redeterminacion switch { "1" => "Pública", "0" => "Privada", "2" => "Cerrada", _ => "No definida" };

        try
        {
            var renglones = await _context.TCotizacionRenglones
                .Where(r => r.IdCotizacion == id)
                .ToListAsync();
            dto.Renglones = _mapper.Map<List<CotizacionRenglonResponseDto>>(renglones);
        }
        catch { dto.Renglones = new(); }

        try
        {
            var proveedores = await _context.TCotizacionProveedores
                .Where(p => p.IdCotizacion == id && p.FecBaja == null)
                .ToListAsync();
            dto.Proveedores = _mapper.Map<List<CotizacionProveedorResponseDto>>(proveedores);
        }
        catch { dto.Proveedores = new(); }
        var itemIds = dto.Detalles.Select(d => d.IdItem).ToList();
        var itemsMap = await _context.TCatalogosBiens
            .Where(i => itemIds.Contains(i.IdItem))
            .ToDictionaryAsync(i => i.IdItem, i => i.NItem);

        var resDetIds = dto.Detalles.Select(d => d.IdReservaDetalle).ToList();

        var resMap = await _context.TReservaDetalles
            .Include(rd => rd.IdReservaNavigation)
            .Where(rd => resDetIds.Contains(rd.IdReservaDet))
            .ToDictionaryAsync(rd => rd.IdReservaDet, rd => new {
                NroReserva = rd.IdReservaNavigation?.NroReserva,
                IdMoneda = rd.IdMoneda
            });

        foreach (var d in dto.Detalles)
        {
            if (itemsMap.TryGetValue(d.IdItem, out var nitem))
            {
                d.NItem = nitem;
            }
            if (resMap.TryGetValue(d.IdReservaDetalle, out var resData))
            {
                d.NroReserva = resData.NroReserva;
                d.IdMoneda = resData.IdMoneda; 
            }
        }

        foreach (var r in dto.Renglones)
        {
            var primerDetalle = dto.Detalles.FirstOrDefault(d => d.IdRenglon == r.IdRenglon);
            r.IdMoneda = primerDetalle?.IdMoneda;
        }

        return Ok(dto);
    }

    public async Task<OperationResponse<CotizacionResponseDto>> CreateAsync(CotizacionRequestDto dto)
    {
        var entity = _mapper.Map<TCotizacion>(dto);
        
        // Logica legacy de inicialización
        entity.IdEstado = 4; // Generado
        entity.IdOrganizacion = GetUserOrganizationId() ?? dto.IdOrganizacion;
        if (entity.IdOrganizacion == 0) entity.IdOrganizacion = 1; // fallback
        
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

        foreach(var ren in entity.Renglones)
            PrepareAuditableEntity(ren, isNew: true);

        if (entity.Renglones.Any())
        {
            var renglonByNro = entity.Renglones.ToDictionary(r => r.NumeroRenglon);
            foreach (var det in entity.Detalles.Where(d => d.IdRenglon.HasValue))
            {
                var nroRenglon = det.IdRenglon.Value;
                if (renglonByNro.TryGetValue(nroRenglon, out var ren))
                {
                    det.IdRenglonNavigation = ren;
                    det.IdRenglon = null;
                }
            }
        }

        _context.TCotizaciones.Add(entity);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_CREADA", "LICITACIONES", new { entity.NroCotizacion, entity.IdCotizacion });

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> UpdateAsync(int id, CotizacionRequestDto dto)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        _mapper.Map(dto, entity);
        PrepareAuditableEntity(entity, isNew: false);
        
        if (entity.Especificacion != null)
            PrepareAuditableEntity(entity.Especificacion, isNew: false);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_MODIFICADA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

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

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_ANULADA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

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

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_PUBLICADA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

        // Publicar SubastaPublicadaEvent con lista de proveedores activos
        try
        {
            await PublishSubastaPublicadaEventAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ No se pudo publicar SubastaPublicadaEvent para Cotización {IdCotizacion}. El flujo continúa.", id);
        }

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    private async Task PublishSubastaPublicadaEventAsync(TCotizacion entity)
    {
        var proveedoresActivos = await _context.TCotizacionProveedores
            .Where(p => p.IdCotizacion == entity.IdCotizacion && p.FecBaja == null)
            .ToListAsync();

        if (proveedoresActivos.Count == 0)
        {
            _logger.LogInformation("⚠️ No hay proveedores activos para Cotización {IdCotizacion}. No se publica SubastaPublicadaEvent.", entity.IdCotizacion);
            return;
        }

        var proveedorIds = proveedoresActivos.Select(p => p.IdProveedor).Distinct().ToList();

        // Resolver emails y nombres desde representantes con LINQ
        var proveedoresInfo = await _context.TProveedoresRepresentantes
            .Include(r => r.IdPersonaNavigation)
            .Where(r => proveedorIds.Contains(r.IdProveedor)
                && r.IdPersonaNavigation.EmailContacto != null
                && r.IdPersonaNavigation.EmailContacto != "")
            .Select(r => new ProveedorInfo(
                r.IdProveedor,
                r.IdPersonaNavigation.EmailContacto,
                r.IdPersonaNavigation.Nombre + " " + r.IdPersonaNavigation.Apellido
            ))
            .ToListAsync();

        if (proveedoresInfo.Count == 0)
        {
            _logger.LogInformation("⚠️ No se encontraron datos de proveedores activos para Cotización {IdCotizacion}.", entity.IdCotizacion);
            return;
        }

        var tipoNombre = entity.IdTipoContratacion.ToDisplayName();

        var subastaEvent = new SubastaPublicadaEvent(
            IdCotizacion: entity.IdCotizacion,
            NroCotizacion: entity.NroCotizacion,
            Titulo: entity.Observacion ?? "Subasta " + entity.NroCotizacion,
            FechaInicio: entity.Especificacion?.FechaInicioSubasta,
            FechaFin: entity.Especificacion?.FechaFinalizacionSubasta,
            TipoContratacion: tipoNombre,
            Proveedores: proveedoresInfo,
            OccuredOn: DateTime.UtcNow
        );

        await _publishEndpoint.Publish(subastaEvent);
        _logger.LogInformation("📧 SubastaPublicadaEvent publicado: Cotización {IdCotizacion}, {Count} proveedores", entity.IdCotizacion, proveedoresInfo.Count);
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

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_FINALIZADA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> DesistirAsync(int id)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null) return NotFound<CotizacionResponseDto>();

        if (entity.IdEstado != 39 && entity.IdEstado != 40)
            return BadRequest<CotizacionResponseDto>("Solo se puede desistir en estado EnviadaPendiente o Finalizada.");

        entity.IdEstado = 47; // Desistir
        PrepareAuditableEntity(entity, isNew: false);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_DESISTIDA", "LICITACIONES", new { entity.IdCotizacion, entity.NroCotizacion });

        // 3. Publicar SubastaDesistidaEvent para email a proveedores
        try
        {
            var proveedores = await _context.TCotizacionProveedores
                .Where(p => p.IdCotizacion == id && p.FecBaja == null)
                .ToListAsync();

            if (proveedores.Count != 0)
            {
                var proveedoresInfo = new List<ProveedorInfo>();
                foreach (var prov in proveedores)
                {
                    var representantes = await GetRepresentantesAsync(prov.IdProveedor);
                    foreach (var (email, nombre) in representantes)
                    {
                        proveedoresInfo.Add(new ProveedorInfo(prov.IdProveedor, email, nombre));
                    }
                }

                if (proveedoresInfo.Count != 0)
                {
                    var tipoNombre = entity.IdTipoContratacion.ToDisplayName();

                    await _publishEndpoint.Publish(new SubastaDesistidaEvent(
                        IdCotizacion: id,
                        NroCotizacion: entity.NroCotizacion ?? "",
                        Titulo: entity.Observacion ?? "Subasta " + (entity.NroCotizacion ?? ""),
                        Motivo: entity.Observacion ?? "Sin motivo especificado",
                        TipoContratacion: tipoNombre,
                        Proveedores: proveedoresInfo,
                        OccuredOn: DateTime.UtcNow
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ No se pudo publicar SubastaDesistidaEvent para Cotización {IdCotizacion}", id);
        }

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    private IQueryable<TCotizacion> GetDashboardBaseQuery(int? idVigencia)
    {
        var query = _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Proveedores)
            .Include(c => c.Ofertas)
            .AsQueryable();

        if (idVigencia.HasValue)
            query = query.Where(c => c.IdVigencia == idVigencia.Value);

        var esAdmin = IsSuperAdmin();
        var proveedorId = GetUserProveedorId();
        if (!esAdmin && proveedorId.HasValue)
        {
            query = query.Where(c =>
                // REGLA 1: Descartar automáticamente si el proveedor desistió (Ganadora == "D")
                !c.Proveedores.Any(p => p.IdProveedor == proveedorId.Value && p.Ganadora == "D" && p.FecBaja == null)
                &&
                (
                    // REGLA 2: Es pública
                    c.Especificacion.Redeterminacion == "1"
                    ||
                    // REGLA 3: Es Privada/Cerrada pero el proveedor ESTÁ invitado
                    (c.Especificacion.Redeterminacion != "1" && c.Proveedores.Any(p => p.IdProveedor == proveedorId.Value && p.FecBaja == null))
                )
            );
        }

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

    public async Task<OperationResponse<List<SubastaDashboardDto>>> BuscarAsync(
        int? idVigencia, int? idEstado, int? idTipoContratacion,
        string? nro, string? expte, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var query = _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .Include(c => c.Proveedores)
            .Include(c => c.Ofertas)
            .AsQueryable();

        // Filtros básicos
        if (idVigencia.HasValue)
            query = query.Where(c => c.IdVigencia == idVigencia.Value);

        if (idEstado.HasValue)
            query = query.Where(c => c.IdEstado == idEstado.Value);

        if (idTipoContratacion.HasValue)
            query = query.Where(c => c.IdTipoContratacion == idTipoContratacion.Value);

        if (!string.IsNullOrWhiteSpace(nro))
            query = query.Where(c => c.NroCotizacion.Contains(nro));

        if (!string.IsNullOrWhiteSpace(expte))
            query = query.Where(c => c.Especificacion.NroExpediente.Contains(expte) || c.Observacion.Contains(expte));

        if (fechaDesde.HasValue)
            query = query.Where(c => c.Especificacion.FechaInicioSubasta >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(c => c.Especificacion.FechaFinalizacionSubasta <= fechaHasta.Value);

        // Extraer contexto del usuario desde el JWT
        var esAdmin = IsSuperAdmin();
        var proveedorId = GetUserProveedorId();

        // Lógica de visibilidad (replica del sistema viejo)
        // Admin ve todo. Proveedor solo ve:
        // - Subastas Públicas (Redeterminacion = "1")
        // - Subastas Privadas (Redeterminacion = "0") donde está invitado
        // - NO ve Cerradas (Redeterminacion = "2") a menos que esté invitado
        // Lógica de visibilidad (replica del sistema viejo)
        if (!esAdmin && proveedorId.HasValue)
        {
            query = query.Where(c =>
                !c.Proveedores.Any(p => p.IdProveedor == proveedorId.Value && p.Ganadora == "D" && p.FecBaja == null)
                &&
                (
                    c.Especificacion.Redeterminacion == "1" // Pública: todos ven
                    || (c.Especificacion.Redeterminacion == "0" && c.Proveedores.Any(p => p.IdProveedor == proveedorId.Value && p.FecBaja == null)) // Privada
                    || (c.Especificacion.Redeterminacion == "2" && c.Proveedores.Any(p => p.IdProveedor == proveedorId.Value && p.FecBaja == null)) // Cerrada
                )
            );
        }

        var data = await query.OrderByDescending(c => c.IdCotizacion).Take(100).ToListAsync();
        return Ok(MapToDashboard(data));
    }

    public async Task<OperationResponse<CotizacionResponseDto>> ProrrogarAsync(int id, int minutos)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .FirstOrDefaultAsync(c => c.IdCotizacion == id);

        if (entity == null || entity.Especificacion == null) return NotFound<CotizacionResponseDto>();

        entity.Especificacion.FechaFinalizacionSubasta = entity.Especificacion.FechaFinalizacionSubasta?.AddMinutes(minutos);
        PrepareAuditableEntity(entity.Especificacion, isNew: false);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "SUBASTA_PRORROGADA", "LICITACIONES", new { entity.IdCotizacion, Minutos = minutos, NuevaFechaFin = entity.Especificacion?.FechaFinalizacionSubasta });

        // 3. Publicar SubastaProrrogadaEvent para email a proveedores
        try
        {
            var proveedores = await _context.TCotizacionProveedores
                .Where(p => p.IdCotizacion == id && p.FecBaja == null)
                .ToListAsync();

            if (proveedores.Count != 0)
            {
                var proveedoresInfo = new List<ProveedorInfo>();
                foreach (var prov in proveedores)
                {
                    var representantes = await GetRepresentantesAsync(prov.IdProveedor);
                    foreach (var (email, nombre) in representantes)
                    {
                        proveedoresInfo.Add(new ProveedorInfo(prov.IdProveedor, email, nombre));
                    }
                }

                if (proveedoresInfo.Count != 0)
                {
                    var tipoNombre = entity.IdTipoContratacion.ToDisplayName();

                    await _publishEndpoint.Publish(new SubastaProrrogadaEvent(
                        IdCotizacion: id,
                        NroCotizacion: entity.NroCotizacion ?? "",
                        Titulo: entity.Observacion ?? "Subasta " + (entity.NroCotizacion ?? ""),
                        FechaFinOriginal: entity.Especificacion?.FechaFinalizacionSubasta?.AddMinutes(-minutos),
                        FechaFinNueva: entity.Especificacion?.FechaFinalizacionSubasta,
                        MinutosAgregados: minutos,
                        TipoContratacion: tipoNombre,
                        Proveedores: proveedoresInfo,
                        OccuredOn: DateTime.UtcNow
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ No se pudo publicar SubastaProrrogadaEvent para Cotización {IdCotizacion}", id);
        }

        return Ok(_mapper.Map<CotizacionResponseDto>(entity));
    }

    public async Task<OperationResponse<bool>> DesistirParticipacionAsync(int idCotizacion)
    {
        var idProveedor = GetUserProveedorId();
        if (!idProveedor.HasValue)
            return BadRequest<bool>("No se pudo identificar al proveedor activo.");

        var participacion = await _context.TCotizacionProveedores
            .FirstOrDefaultAsync(p => p.IdCotizacion == idCotizacion && p.IdProveedor == idProveedor.Value && p.FecBaja == null);

        if (participacion == null)
            return BadRequest<bool>("No estás asignado como proveedor a esta subasta.");

        participacion.Ganadora = "D";
        PrepareAuditableEntity(participacion, isNew: false);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "PROVEEDOR_DESISTE_SUBASTA", "LICITACIONES", new { IdCotizacion = idCotizacion, IdProveedor = idProveedor });

        return Ok(true);
    }

    public async Task<OperationResponse<MetricasAhorroDto>> GetMetricasAhorroAsync(int idCotizacion)
    {
        var cotizacion = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion);

        if (cotizacion == null) return NotFound<MetricasAhorroDto>();

        bool isRenglon = cotizacion.Especificacion?.CriterioAdjudicacion == 1;
        decimal presupuestoBaseTotal = 0;
        decimal mejorOfertaTotal = 0;

        if (isRenglon)
        {
            var renglonesIds = cotizacion.Detalles.Where(d => d.IdRenglon.HasValue).Select(d => d.IdRenglon.Value).Distinct().ToList();

            foreach (var idRenglon in renglonesIds)
            {
                decimal baseRenglon = cotizacion.Detalles.Where(d => d.IdRenglon == idRenglon).Sum(d => d.ImporteBase * d.Cantidad);
                presupuestoBaseTotal += baseRenglon;
                
                var minOferta = await _context.TOfertasSubastas
                    .Where(o => o.IdRenglon == idRenglon && o.FecBaja == null)
                    .MinAsync(o => (decimal?)o.Monto);

                mejorOfertaTotal += minOferta ?? baseRenglon;
            }
        }
        else
        {
            foreach (var detalle in cotizacion.Detalles)
            {
                decimal baseItem = detalle.ImporteBase * detalle.Cantidad;
                presupuestoBaseTotal += baseItem;

                var minOferta = await _context.TOfertasSubastas
                    .Where(o => o.IdCotizacionDetalle == detalle.IdCotizacionDetalle && o.FecBaja == null)
                    .MinAsync(o => (decimal?)o.Monto);

                mejorOfertaTotal += (minOferta ?? detalle.ImporteBase) * detalle.Cantidad;
            }
        }

        decimal ahorroPorcentaje = 0;
        if (presupuestoBaseTotal > 0)
        {
            ahorroPorcentaje = ((presupuestoBaseTotal - mejorOfertaTotal) / presupuestoBaseTotal) * 100m;
        }

        return Ok(new MetricasAhorroDto
        {
            PresupuestoBase = Math.Round(presupuestoBaseTotal, 2),
            MejorOfertaFinal = Math.Round(mejorOfertaTotal, 2),
            AhorroPorcentaje = Math.Round(ahorroPorcentaje, 2)
        });
    }


    public async Task<OperationResponse<List<SubastaPublicaListDto>>> GetSubastasPublicasActivasAsync()
    {
        var now = DateTime.Now;

        var data = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.IdUnidadAdmNavigation)
            .Include(c => c.Detalles)
            .Include(c => c.Renglones)
            .Include(c => c.Ofertas)
            .Where(c => c.IdEstado == 39
                     && c.FecBaja == null
                     && c.Especificacion.Redeterminacion == "1"
                     && c.Especificacion.FechaInicioSubasta <= now
                     && c.Especificacion.FechaFinalizacionSubasta >= now)
            .OrderBy(c => c.Especificacion.FechaFinalizacionSubasta)
            .ToListAsync();

        var resDetIds = data.SelectMany(c => c.Detalles).Select(d => d.IdReservaDetalle).Distinct().ToList();

        var monedasMap = await _context.TReservaDetalles
            .Include(rd => rd.IdMonedaNavigation)
            .Where(rd => resDetIds.Contains(rd.IdReservaDet))
            .ToDictionaryAsync(
                rd => rd.IdReservaDet,
                rd => rd.IdMonedaNavigation?.Simbolo ?? "ARS" 
            );

        var result = new List<SubastaPublicaListDto>();

        foreach (var c in data)
        {
            bool isRenglon = c.Especificacion?.CriterioAdjudicacion == 1;
            decimal precioBaseTotal = 0;
            int cantItems = 0;
            var ofertasActivas = c.Ofertas.Where(o => o.FecBaja == null).ToList();

            // 1. Calcular Precio Base y Cantidad de Ítems
            if (isRenglon)
            {
                cantItems = c.Renglones.Count(r => r.FecBaja == null);
                foreach (var r in c.Renglones.Where(r => r.FecBaja == null))
                {
                    precioBaseTotal += c.Detalles.Where(d => d.IdRenglon == r.IdRenglon && d.FecBaja == null).Sum(d => d.ImporteBase * d.Cantidad);
                }
            }
            else
            {
                var detallesActivos = c.Detalles.Where(d => d.FecBaja == null).ToList();
                cantItems = detallesActivos.Count;
                precioBaseTotal = detallesActivos.Sum(d => d.ImporteBase * d.Cantidad);
            }

            // 2. Calcular Mejor Oferta Total
            decimal? mejorOfertaTotal = null;
            if (ofertasActivas.Any())
            {
                mejorOfertaTotal = 0;
                if (isRenglon)
                {
                    foreach (var rId in c.Renglones.Select(r => r.IdRenglon))
                    {
                        var best = c.IdTipoContratacion == 9
                            ? ofertasActivas.Where(o => o.IdRenglon == rId).Max(o => (decimal?)o.Monto)
                            : ofertasActivas.Where(o => o.IdRenglon == rId).Min(o => (decimal?)o.Monto);
                        mejorOfertaTotal += best ?? c.Detalles.Where(d => d.IdRenglon == rId).Sum(d => d.ImporteBase * d.Cantidad);
                    }
                }
                else
                {
                    foreach (var d in c.Detalles.Where(det => det.FecBaja == null))
                    {
                        var best = c.IdTipoContratacion == 9
                            ? ofertasActivas.Where(o => o.IdCotizacionDetalle == d.IdCotizacionDetalle).Max(o => (decimal?)o.Monto)
                            : ofertasActivas.Where(o => o.IdCotizacionDetalle == d.IdCotizacionDetalle).Min(o => (decimal?)o.Monto);
                        mejorOfertaTotal += (best ?? d.ImporteBase) * d.Cantidad;
                    }
                }
            }

            string simboloMoneda = "ARS";
            var primerDetalle = c.Detalles.FirstOrDefault();
            if (primerDetalle != null && monedasMap.TryGetValue(primerDetalle.IdReservaDetalle, out var simbolo))
            {
                simboloMoneda = simbolo;
            }

            result.Add(new SubastaPublicaListDto
            {
                IdCotizacion = c.IdCotizacion,
                NroCotizacion = c.NroCotizacion,
                Tipo = c.IdTipoContratacion.ToDisplayName(),
                Titulo = c.Observacion ?? "Subasta " + c.NroCotizacion,
                Estado = "En Curso",
                UnidadAdm = c.IdUnidadAdmNavigation?.NombreUnidadAdm ?? "",
                PrecioBase = Math.Round(precioBaseTotal, 2),
                MejorOfertaActual = mejorOfertaTotal.HasValue ? Math.Round(mejorOfertaTotal.Value, 2) : null,
                FechaInicio = c.Especificacion?.FechaInicioSubasta,
                FechaFin = c.Especificacion?.FechaFinalizacionSubasta,
                Moneda = simboloMoneda,
                CantItems = cantItems,
                CantOfertas = ofertasActivas.Count
            });
        }

        return Ok(result);
    }

    public async Task<OperationResponse<SubastaPublicaDetalleDto>> GetDetalleSubastaPublicaAsync(int idCotizacion)
    {
        var entity = await _context.TCotizaciones
            .Include(c => c.Especificacion)
            .Include(c => c.Detalles)
            .Include(c => c.Renglones)
            .Include(c => c.IdUnidadAdmNavigation)
            .Include(c => c.Ofertas) // Incluimos las ofertas para el detalle
            .FirstOrDefaultAsync(c => c.IdCotizacion == idCotizacion
                                   && c.IdEstado == 39
                                   && c.FecBaja == null
                                   && c.Especificacion.Redeterminacion == "1");

        if (entity == null || entity.Especificacion == null)
            return NotFound<SubastaPublicaDetalleDto>();

        // Mapeo del Catálogo de Bienes (Ahora traemos Nombre y Código)
        var itemIds = entity.Detalles.Select(d => d.IdItem).Distinct().ToList();
        var itemsMap = await _context.TCatalogosBiens
            .Where(i => itemIds.Contains(i.IdItem))
            .ToDictionaryAsync(i => i.IdItem, i => new { i.NItem, i.Codigo });

        // Identificamos la moneda
        string simboloMoneda = "ARS";
        var primerDetalle = entity.Detalles.FirstOrDefault();
        if (primerDetalle != null)
        {
            var reservaDetalle = await _context.TReservaDetalles
                .Include(rd => rd.IdMonedaNavigation)
                .FirstOrDefaultAsync(rd => rd.IdReservaDet == primerDetalle.IdReservaDetalle);

            if (reservaDetalle?.IdMonedaNavigation != null)
                simboloMoneda = reservaDetalle.IdMonedaNavigation.Simbolo;
        }

        bool isRenglon = entity.Especificacion.CriterioAdjudicacion == 1;
        var items = new List<ItemPublicoDto>();
        var todasLasOfertas = entity.Ofertas.Where(o => o.FecBaja == null).ToList();

        decimal precioBaseTotal = 0;
        decimal? mejorOfertaTotal = todasLasOfertas.Any() ? 0m : null;

        if (isRenglon)
        {
            foreach (var ren in entity.Renglones.Where(r => r.FecBaja == null))
            {
                var detallesRenglon = entity.Detalles.Where(d => d.IdRenglon == ren.IdRenglon && d.FecBaja == null).ToList();

                decimal cantidad = detallesRenglon.Sum(d => d.Cantidad);
                decimal precioBase = detallesRenglon.Sum(d => d.ImporteBase * d.Cantidad);
                precioBaseTotal += precioBase;

                // Buscar mejor oferta
                decimal? mejorOferta = null;
                var ofertasRenglon = todasLasOfertas.Where(o => o.IdRenglon == ren.IdRenglon).ToList();
                if (ofertasRenglon.Count != 0)
                {
                    mejorOferta = entity.IdTipoContratacion == 9
                        ? ofertasRenglon.Max(o => (decimal?)o.Monto)
                        : ofertasRenglon.Min(o => (decimal?)o.Monto);
                }

                if (mejorOfertaTotal != null) mejorOfertaTotal += mejorOferta ?? precioBase;

                items.Add(new ItemPublicoDto
                {
                    IdElemento = ren.IdRenglon,
                    EsRenglon = true,
                    Codigo = ren.NumeroRenglon.ToString(),
                    Descripcion = ren.Descripcion,
                    Unidad = "GL", // Global para renglones
                    Cantidad = cantidad,
                    PrecioBase = Math.Round(precioBase, 2),
                    MejorOfertaActual = mejorOferta.HasValue ? Math.Round(mejorOferta.Value, 2) : null
                });
            }
        }
        else
        {
            foreach (var det in entity.Detalles.Where(d => d.FecBaja == null))
            {
                decimal precioBase = det.ImporteBase * det.Cantidad;
                precioBaseTotal += precioBase;

                decimal? mejorOferta = null;
                var ofertasItem = todasLasOfertas.Where(o => o.IdCotizacionDetalle == det.IdCotizacionDetalle).ToList();
                if (ofertasItem.Count != 0)
                {
                    mejorOferta = entity.IdTipoContratacion == 9
                        ? ofertasItem.Max(o => (decimal?)o.Monto)
                        : ofertasItem.Min(o => (decimal?)o.Monto);
                }

                if (mejorOfertaTotal != null) mejorOfertaTotal += (mejorOferta ?? det.ImporteBase) * det.Cantidad;

                itemsMap.TryGetValue(det.IdItem, out var catalogData);

                items.Add(new ItemPublicoDto
                {
                    IdElemento = det.IdCotizacionDetalle,
                    EsRenglon = false,
                    Codigo = catalogData?.Codigo ?? "-",
                    Descripcion = catalogData?.NItem ?? $"Ítem #{det.IdItem}",
                    Unidad = "UN", // Unidad por defecto
                    Cantidad = det.Cantidad,
                    PrecioBase = Math.Round(det.ImporteBase, 2),
                    MejorOfertaActual = mejorOferta.HasValue ? Math.Round(mejorOferta.Value, 2) : null
                });
            }
        }

        // Extraemos las ofertas ordenadas por fecha para el gráfico
        var historial = todasLasOfertas
            .OrderBy(o => o.FechaOferta)
            .Select(o => new OfertaPublicaDto
            {
                Monto = o.Monto,
                Fecha = o.FechaOferta,
                IdCotizacionDetalle = o.IdCotizacionDetalle,
                IdRenglon = o.IdRenglon
            }).ToList();

        var dto = new SubastaPublicaDetalleDto
        {
            IdCotizacion = entity.IdCotizacion,
            NroCotizacion = entity.NroCotizacion,
            Tipo = entity.IdTipoContratacion.ToDisplayName(),
            Titulo = entity.Observacion ?? "Subasta " + entity.NroCotizacion,
            Estado = "En Curso",
            UnidadAdm = entity.IdUnidadAdmNavigation?.NombreUnidadAdm ?? "",
            PrecioBase = Math.Round(precioBaseTotal, 2),
            MejorOfertaActual = mejorOfertaTotal.HasValue ? Math.Round(mejorOfertaTotal.Value, 2) : null,
            FechaInicio = entity.Especificacion.FechaInicioSubasta,
            FechaFin = entity.Especificacion.FechaFinalizacionSubasta,
            Moneda = simboloMoneda,
            CantOfertas = todasLasOfertas.Count,
            Items = items,
            HistorialOfertas = historial
        };

        return Ok(dto);
    }

    private string GetEstadoNombre(int idEstado) => idEstado switch
    {
        4 => "Generado",
        20 => "Anulado",
        39 => "Enviada Pendiente",
        40 => "Finalizada",
        47 => "Desistida",
        _ => "Desconocido"
    };

    private List<SubastaDashboardDto> MapToDashboard(List<TCotizacion> data)
    {
        return data.Select(c => new SubastaDashboardDto
        {
            IdCotizacion = c.IdCotizacion,
            IdEstado = c.IdEstado,
            NroCotizacion = c.NroCotizacion,
            IdTipoContratacion = c.IdTipoContratacion,
            TipoContratacion = c.IdTipoContratacion.ToDisplayName(),
            Tipo = c.IdTipoContratacion.ToDisplayName(),
            Estado = GetEstadoNombre(c.IdEstado),
            Titulo = c.Observacion ?? "Subasta " + c.NroCotizacion,
            UnidadAdm = c.IdUnidadAdmNavigation?.NombreUnidadAdm ?? "",
            ObjetoContratacion = c.Observacion ?? "",
            CriterioAdjudicacion = c.Especificacion?.CriterioAdjudicacion,
            Redeterminacion = c.Especificacion?.Redeterminacion,
            FechaInicio = c.Especificacion?.FechaInicioSubasta,
            FechaFin = c.Especificacion?.FechaFinalizacionSubasta,
            FechaConsulta = c.Especificacion?.FechaLimiteConsultas,
            FechaFinSubasta = c.Especificacion?.FechaFinalizacionSubasta,
            VerInformeFinal = c.IdEstado == 40, // Finalizada

            MostrarBotonMejora = c.Especificacion?.MostrarBotonMejora ?? false,
            TipoSobre = c.Especificacion?.TipoSobre,
            FechaLimiteImpugnar = c.Especificacion?.FechaLimiteImpugnar,
            FechaAperturaSobreUno = c.Especificacion?.FechaAperturaSobreUno,
            FechaAperturaSobreDos = c.Especificacion?.FechaAperturaSobreDos,
            CantOfertas = c.Ofertas != null ? c.Ofertas.Count(o => o.FecBaja == null) : 0
        }).ToList();
    }

    private async Task<List<(string Email, string NombrePersona)>> GetRepresentantesAsync(int idProveedor)
        => await _proveedorRepresentanteService.GetRepresentantesAsync(idProveedor);
}
