namespace PortalSubastas.Licitaciones.Application.RequestDto.Ganador;

public class GanadorRequestDto
{
    public int IdCotizacion { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int IdProveedor { get; set; }
    public decimal MontoGanador { get; set; }
    public decimal CantidadAdjudicada { get; set; }
}
