using PortalSubastas.Licitaciones.Application.RequestDto.Reserva;
using PortalSubastas.Licitaciones.Application.ResponseDto.Estados;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reserva;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class ReservaService : BaseService, IReservaService
{
    private new readonly PortalSubastasContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public ReservaService(
        PortalSubastasContext context,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        IPublishEndpoint publishEndpoint)
        : base(context, mapper, httpContextAccessor, cache)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OperationResponse<List<ReservaResponseDto>>> GetAllAsync(ReservaFilterDto? filtros = null)
    {
        var query = _context.TReservas
            .Include(r => r.IdEstadoNavigation)
            .Include(r => r.IdUnidadAdmNavigation)
            .Include(r => r.IdSubResponsableNavigation)
            .AsQueryable();

        if (filtros != null)
        {
            if (filtros.IdUnidadAdm.HasValue)
                query = query.Where(r => r.IdUnidadAdm == filtros.IdUnidadAdm.Value);

            if (filtros.IdVigencia.HasValue)
                query = query.Where(r => r.IdVigencia == filtros.IdVigencia.Value);

            if (!string.IsNullOrWhiteSpace(filtros.NroReserva))
                query = query.Where(r => r.NroReserva.Contains(filtros.NroReserva));
        }

        var reservas = await query
            .OrderByDescending(r => r.IdReserva)
            .ToListAsync();

        return Ok(_mapper.Map<List<ReservaResponseDto>>(reservas));
    }

    public async Task<OperationResponse<ReservaResponseDto>> GetByIdAsync(int id)
    {
        var reserva = await _context.TReservas
            .Include(r => r.IdEstadoNavigation)
            .Include(r => r.IdUnidadAdmNavigation)
            .Include(r => r.IdSubResponsableNavigation)
            .FirstOrDefaultAsync(r => r.IdReserva == id);

        if (reserva == null)
            return NotFound<ReservaResponseDto>();

        return Ok(_mapper.Map<ReservaResponseDto>(reserva));
    }

    public async Task<OperationResponse<ReservaResponseDto>> CreateAsync(ReservaRequestDto dto)
    {
        var vigenciaActiva = await _context.TVigencias
            .FirstOrDefaultAsync(v => v.ActivoEjecucion == true);

        if (vigenciaActiva == null)
            return BadRequest<ReservaResponseDto>("No hay una vigencia activa configurada.");

        var unidadAdm = await _context.TUnidadesAdministrativas
            .FirstOrDefaultAsync(u => u.IdUnidadAdm == dto.IdUnidadAdm);

        if (unidadAdm == null)
            return BadRequest<ReservaResponseDto>("La unidad administrativa no existe.");

        var nroSecuencial = await _context.TReservas
            .Where(r => r.IdVigencia == vigenciaActiva.IdVigencia && r.IdOrganizacion == unidadAdm.IdOrganizacion)
            .Select(r => r.NroReserva)
            .ToListAsync();

        var maxNro = nroSecuencial
            .Select(n => int.TryParse(n.Substring(5), out var num) ? num : 0)
            .DefaultIfEmpty(0)
            .Max();

        var nuevoNumero = maxNro + 1;
        var nroReserva = $"{vigenciaActiva.Ejercicio}/{nuevoNumero.ToString().PadLeft(6, '0')}";

        var entity = _mapper.Map<TReserva>(dto);
        entity.NroReserva = nroReserva;
        entity.IdVigencia = vigenciaActiva.IdVigencia;
        entity.IdOrganizacion = unidadAdm.IdOrganizacion;
        entity.IdEstado = 1; // GENERADO

        PrepareAuditableEntity(entity, isNew: true);
        _context.TReservas.Add(entity);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "CREAR_NOTA_PEDIDO", "LICITACIONES",
            new { Mensaje = $"Se creó una nueva nota de pedido: {nroReserva}" });

        return await GetByIdAsync(entity.IdReserva);
    }

    public async Task<OperationResponse<ReservaResponseDto>> UpdateAsync(int id, ReservaRequestDto dto)
    {
        var reserva = await _context.TReservas.FindAsync(id);
        if (reserva == null)
            return NotFound<ReservaResponseDto>();

        reserva.IdUnidadAdm = dto.IdUnidadAdm;
        reserva.IdSubResponsable = dto.IdSubResponsable;
        reserva.ComentariosUsuarios = dto.ComentariosUsuarios;
        reserva.FechaReserva = dto.FechaReserva;

        var result = await UpdateAsync<TReserva, ReservaResponseDto>(reserva, _context);

        await PublishSystemLogAsync(_publishEndpoint, "MODIFICAR_NOTA_PEDIDO", "LICITACIONES",
            new { Mensaje = $"Se modificó la nota de pedido: {reserva.NroReserva}" });

        return result;
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var reserva = await _context.TReservas.FindAsync(id);
        if (reserva == null)
            return NotFound<bool>();

        var result = await DeleteAsync(reserva, _context);

        await PublishSystemLogAsync(_publishEndpoint, "ELIMINAR_NOTA_PEDIDO", "LICITACIONES",
            new { Mensaje = $"Se eliminó (baja lógica) la nota de pedido: {reserva.NroReserva}" });

        return result;
    }

    public async Task<OperationResponse<ReservaResponseDto>> AutorizarAsync(int id, AutorizarReservaDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.MotivoAutorizacion))
            return BadRequest<ReservaResponseDto>("El motivo de autorización es obligatorio.");

        var reserva = await _context.TReservas.FindAsync(id);
        if (reserva == null)
            return NotFound<ReservaResponseDto>();

        var tieneDetalles = await _context.TReservaDetalles
            .AnyAsync(d => d.IdReserva == id && d.FecBaja == null);

        if (!tieneDetalles)
            return BadRequest<ReservaResponseDto>("La nota de pedido debe tener al menos un bien o servicio para ser autorizada.");

        reserva.IdEstado = 3; // AUTORIZADO
        reserva.MotivoAutorizacion = dto.MotivoAutorizacion;

        PrepareAuditableEntity(reserva, isNew: false);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "AUTORIZACION_NOTA_PEDIDO", "LICITACIONES",
            new
            {
                Mensaje = $"Se autorizó la nota de pedido {reserva.NroReserva}.",
                Motivo = dto.MotivoAutorizacion
            });

        return Ok(_mapper.Map<ReservaResponseDto>(reserva));
    }

    public async Task<OperationResponse<ReservaResponseDto>> ClonarAsync(int id)
    {
        var original = await _context.TReservas
            .Include(r => r.TReservaDetalles.Where(d => d.FecBaja == null))
            .FirstOrDefaultAsync(r => r.IdReserva == id);

        if (original == null)
            return NotFound<ReservaResponseDto>();

        var vigenciaActiva = await _context.TVigencias
            .FirstOrDefaultAsync(v => v.ActivoEjecucion == true);

        if (vigenciaActiva == null)
            return BadRequest<ReservaResponseDto>("No hay una vigencia activa configurada.");

        var nroSecuencial = await _context.TReservas
            .Where(r => r.IdVigencia == vigenciaActiva.IdVigencia && r.IdOrganizacion == original.IdOrganizacion)
            .Select(r => r.NroReserva)
            .ToListAsync();

        var maxNro = nroSecuencial
            .Select(n => int.TryParse(n.Substring(5), out var num) ? num : 0)
            .DefaultIfEmpty(0)
            .Max();

        var nuevoNumero = maxNro + 1;
        var nroReserva = $"{vigenciaActiva.Ejercicio}/{nuevoNumero.ToString().PadLeft(6, '0')}";

        var clon = new TReserva
        {
            NroReserva = nroReserva,
            IdVigencia = vigenciaActiva.IdVigencia,
            IdUnidadAdm = original.IdUnidadAdm,
            IdSubResponsable = original.IdSubResponsable,
            IdOrganizacion = original.IdOrganizacion,
            IdEstado = 1, // GENERADO
            FechaReserva = DateOnly.FromDateTime(DateTime.Now),
            ComentariosUsuarios = original.ComentariosUsuarios,
            IdReservaClonar = original.IdReserva
        };

        PrepareAuditableEntity(clon, isNew: true);
        _context.TReservas.Add(clon);
        await _context.SaveChangesAsync();

        foreach (var detalle in original.TReservaDetalles)
        {
            var clonDetalle = new TReservaDetalle
            {
                IdReserva = clon.IdReserva,
                IdCatProg = detalle.IdCatProg,
                IdItem = detalle.IdItem,
                IdMoneda = detalle.IdMoneda,
                IdObjetoGasto = detalle.IdObjetoGasto,
                Cantidad = detalle.Cantidad,
                Importe = detalle.Importe,
                ImporteFuturo = detalle.ImporteFuturo,
                EspecificacionesTecnicas = detalle.EspecificacionesTecnicas,
                FechaEntrega = detalle.FechaEntrega,
                PlazoEntregaDesde = detalle.PlazoEntregaDesde,
                PlazoEntregaHasta = detalle.PlazoEntregaHasta,
                IdEstado = detalle.IdEstado,
                IdReservaDetClonar = detalle.IdReservaDet
            };

            PrepareAuditableEntity(clonDetalle, isNew: true);
            _context.TReservaDetalles.Add(clonDetalle);
        }

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "CLONAR_NOTA_PEDIDO", "LICITACIONES",
            new { Mensaje = $"Se clonó la nota de pedido {original.NroReserva} resultando en la nueva nota {nroReserva}." });

        return Ok(_mapper.Map<ReservaResponseDto>(clon));
    }

    public async Task<OperationResponse<List<EstadoDto>>> GetEstadosAsync()
    {
        var estados = await _context.TEstados
            .Where(e => e.FecBaja == null)
            .Select(e => new EstadoDto { IdEstado = e.IdEstado, Descripcion = e.Descripcion })
            .ToListAsync();

        return Ok(estados);
    }
}