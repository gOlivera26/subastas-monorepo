namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reserva;

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
}
