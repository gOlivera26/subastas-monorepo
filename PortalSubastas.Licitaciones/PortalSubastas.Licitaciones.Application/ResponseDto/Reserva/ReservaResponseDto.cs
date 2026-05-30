namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reserva;

public class ReservaDetalleItemDto
{
    public int IdReservaDet { get; set; }
    public int IdReserva { get; set; }
    public int IdItem { get; set; }
    public string? NItem { get; set; }
    public string? NroReserva { get; set; }
    public decimal Cantidad { get; set; }
    public decimal Importe { get; set; }
    public string? NombreUnidadAdm { get; set; }
    public string? NombreSubResponsable { get; set; }
    public string? SimboloMoneda { get; set; }
    public decimal CantidadRestante { get; set; }
    public DateTime? FecIng { get; set; }
}

public class ReservaResponseDto
{
    public int IdReserva { get; set; }
    public string NroReserva { get; set; } = string.Empty;
    public int IdVigencia { get; set; }
    public int IdUnidadAdm { get; set; }
    public int? IdSubResponsable { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? IdEstado { get; set; }
    public string? DescripcionEstado { get; set; }
    public DateOnly FechaReserva { get; set; }
    public string? ComentariosUsuarios { get; set; }
    public int? IdReservaClonar { get; set; }
    public string? NombreUnidadAdm { get; set; }
    public string? NombreSubResponsable { get; set; }
    public DateTime? FecIng { get; set; }
    public DateTime? FecBaja { get; set; }
    public List<ReservaDetalleItemDto> Detalles { get; set; } = new();
}
