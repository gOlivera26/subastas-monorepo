namespace PortalSubastas.Licitaciones.Application.ResponseDto.ReservaDetalle;

public class ReservaDetalleResponseDto
{
    public int IdReservaDet { get; set; }
    public int IdReserva { get; set; }
    public int? IdCatProg { get; set; }
    public string? NombreCategoriaProgramatica { get; set; }
    public int IdItem { get; set; }
    public string? NombreBien { get; set; }
    public int? IdMoneda { get; set; }
    public string? NombreMoneda { get; set; }
    public int? IdObjetoGasto { get; set; }
    public string? NombreObjetoGasto { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal? Importe { get; set; }
    public decimal? ImporteFuturo { get; set; }
    public decimal? Total => Cantidad * Importe;
    public string? EspecificacionesTecnicas { get; set; }
    public DateOnly? FechaEntrega { get; set; }
    public DateOnly? PlazoEntregaDesde { get; set; }
    public DateOnly? PlazoEntregaHasta { get; set; }
    public int? IdEstado { get; set; }
    public string? DescripcionEstado { get; set; }
    public DateTime? FecIng { get; set; }
    public DateTime? FecBaja { get; set; }
}
