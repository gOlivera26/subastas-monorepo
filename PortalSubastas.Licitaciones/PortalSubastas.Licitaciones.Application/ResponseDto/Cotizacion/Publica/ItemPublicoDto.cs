namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion.Publica;

public class ItemPublicoDto
{
    public int IdElemento { get; set; }
    public bool EsRenglon { get; set; }
    public string Codigo { get; set; }
    public string Descripcion { get; set; }
    public string Unidad { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal? MejorOfertaActual { get; set; }
}