namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion.Publica;

public class ItemPublicoDto
{
    /// <summary>Id del elemento (IdCotizacionDetalle o IdRenglon según EsRenglon)</summary>
    public int IdElemento { get; set; }
    public bool EsRenglon { get; set; }
    public string Nombre { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal? MejorOfertaActual { get; set; }
}
