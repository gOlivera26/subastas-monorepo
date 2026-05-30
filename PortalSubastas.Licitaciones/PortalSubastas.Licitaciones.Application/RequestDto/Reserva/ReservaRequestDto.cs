namespace PortalSubastas.Licitaciones.Application.RequestDto.Reserva;

public class ReservaRequestDto
{
    public int? IdVigencia { get; set; }
    public int IdUnidadAdm { get; set; }
    public int? IdSubResponsable { get; set; }
    public int? IdOrganizacion { get; set; }
    public DateOnly FechaReserva { get; set; }
    public string? ComentariosUsuarios { get; set; }
    public int? IdReservaClonar { get; set; }
}
