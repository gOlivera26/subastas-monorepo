namespace PortalSubastas.Licitaciones.Application.RequestDto.ReservaDetalle;

public class ReservaDetalleRequestDto
{
    public int IdReserva { get; set; }
    public int? IdCatProg { get; set; }
    public int IdItem { get; set; }
    public int? IdMoneda { get; set; }
    public int? IdObjetoGasto { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal? Importe { get; set; }
    public decimal? ImporteFuturo { get; set; }
    public string? EspecificacionesTecnicas { get; set; }
    public DateOnly? FechaEntrega { get; set; }
    public DateOnly? PlazoEntregaDesde { get; set; }
    public DateOnly? PlazoEntregaHasta { get; set; }
}
