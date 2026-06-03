namespace PortalSubastas.Licitaciones.Application.RequestDto.OfertaSubasta;

public class OfertaItemRequestDto
{
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public decimal Monto { get; set; }
    public int? IdMonedaOferta { get; set; }
    public decimal Cantidad { get; set; }
}
