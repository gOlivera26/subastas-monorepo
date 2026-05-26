namespace PortalSubastas.Licitaciones.Application.ResponseDto.Ganador;

public class GanadorResponseDto
{
    public int IdGanador { get; set; }
    public int IdCotizacion { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int IdProveedor { get; set; }
    public decimal MontoGanador { get; set; }
    public decimal CantidadAdjudicada { get; set; }
    public string NroCotizacion { get; set; }
}
