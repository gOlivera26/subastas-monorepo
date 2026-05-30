using PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.ResponseDto.ReservaDetalle;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;
using PortalSubastas.Licitaciones.Domain.Models;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public class ReservaDetalleService : BaseService, IReservaDetalleService
{
    private new readonly PortalSubastasContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public ReservaDetalleService(
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

    public async Task<OperationResponse<List<ReservaDetalleResponseDto>>> GetByReservaIdAsync(int reservaId)
    {
        var detalles = await _context.TReservaDetalles
            .Include(d => d.IdCatProgNavigation)
            .Include(d => d.IdItemNavigation)
            .Include(d => d.IdMonedaNavigation)
            .Include(d => d.IdObjetoGastoNavigation)
            .Include(d => d.IdEstadoNavigation)
            .Where(d => d.IdReserva == reservaId && d.FecBaja == null)
            .OrderBy(d => d.IdReservaDet)
            .ToListAsync();

        return Ok(_mapper.Map<List<ReservaDetalleResponseDto>>(detalles));
    }

    public async Task<OperationResponse<ReservaDetalleResponseDto>> GetByIdAsync(int id)
    {
        var detalle = await _context.TReservaDetalles
            .Include(d => d.IdCatProgNavigation)
            .Include(d => d.IdItemNavigation)
            .Include(d => d.IdMonedaNavigation)
            .Include(d => d.IdObjetoGastoNavigation)
            .Include(d => d.IdEstadoNavigation)
            .FirstOrDefaultAsync(d => d.IdReservaDet == id);

        if (detalle == null)
            return NotFound<ReservaDetalleResponseDto>();

        return Ok(_mapper.Map<ReservaDetalleResponseDto>(detalle));
    }

    public async Task<OperationResponse<ReservaDetalleResponseDto>> CreateAsync(ReservaDetalleRequestDto dto)
    {
        var reserva = await _context.TReservas.FindAsync(dto.IdReserva);
        if (reserva == null)
            return BadRequest<ReservaDetalleResponseDto>("La nota de pedido no existe.");

        if (reserva.IdEstado == 3) // AUTORIZADO
            return BadRequest<ReservaDetalleResponseDto>("No se pueden modificar notas de pedido autorizadas.");

        var entity = _mapper.Map<TReservaDetalle>(dto);
        entity.IdEstado = 1; // GENERADO

        PrepareAuditableEntity(entity, isNew: true);
        _context.TReservaDetalles.Add(entity);
        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "AGREGAR_ITEM_NOTA", "LICITACIONES",
            new { Mensaje = $"Se agregó un ítem a la nota de pedido {reserva.NroReserva}. (Item ID: {dto.IdItem})" });

        return Ok(_mapper.Map<ReservaDetalleResponseDto>(entity));
    }

    public async Task<OperationResponse<ReservaDetalleResponseDto>> UpdateAsync(int id, ReservaDetalleRequestDto dto)
    {
        var detalle = await _context.TReservaDetalles.FindAsync(id);
        if (detalle == null)
            return NotFound<ReservaDetalleResponseDto>();

        var reserva = await _context.TReservas.FindAsync(detalle.IdReserva);
        if (reserva != null && reserva.IdEstado == 3) // AUTORIZADO
            return BadRequest<ReservaDetalleResponseDto>("No se pueden modificar notas de pedido autorizadas.");

        detalle.IdCatProg = dto.IdCatProg;
        detalle.IdItem = dto.IdItem;
        detalle.IdMoneda = dto.IdMoneda;
        detalle.IdObjetoGasto = dto.IdObjetoGasto;
        detalle.Cantidad = dto.Cantidad;
        detalle.Importe = dto.Importe;
        detalle.ImporteFuturo = dto.ImporteFuturo;
        detalle.EspecificacionesTecnicas = dto.EspecificacionesTecnicas;
        detalle.FechaEntrega = dto.FechaEntrega;
        detalle.PlazoEntregaDesde = dto.PlazoEntregaDesde;
        detalle.PlazoEntregaHasta = dto.PlazoEntregaHasta;

        var result = await UpdateAsync<TReservaDetalle, ReservaDetalleResponseDto>(detalle, _context);

        await PublishSystemLogAsync(_publishEndpoint, "MODIFICAR_ITEM_NOTA", "LICITACIONES",
            new { Mensaje = $"Se modificó un ítem de la nota de pedido {reserva?.NroReserva ?? "Desconocida"}. (Detalle ID: {id})" });

        return result;
    }

    public async Task<OperationResponse<bool>> DeleteAsync(int id)
    {
        var detalle = await _context.TReservaDetalles.FindAsync(id);
        if (detalle == null)
            return NotFound<bool>();

        var reserva = await _context.TReservas.FindAsync(detalle.IdReserva);
        if (reserva != null && reserva.IdEstado == 3) // AUTORIZADO
            return BadRequest<bool>("No se pueden modificar notas de pedido autorizadas.");

        var result = await DeleteAsync(detalle, _context);

        await PublishSystemLogAsync(_publishEndpoint, "ELIMINAR_ITEM_NOTA", "LICITACIONES",
            new { Mensaje = $"Se eliminó un ítem de la nota de pedido {reserva?.NroReserva ?? "Desconocida"}. (Detalle ID: {id})" });

        return result;
    }

    public async Task<OperationResponse<bool>> DesautorizarAsync(int id)
    {
        var detalle = await _context.TReservaDetalles.FindAsync(id);
        if (detalle == null) return NotFound<bool>();

        detalle.IdEstado = 7; // 7 = Anulado
        PrepareAuditableEntity(detalle, isNew: false);

        await _context.SaveChangesAsync();

        await PublishSystemLogAsync(_publishEndpoint, "DESAUTORIZAR_ITEM", "LICITACIONES",
            new { Mensaje = $"Se desautorizó/anuló el ítem ID {id} desde la creación de subasta." });

        return Ok(true);
    }
}