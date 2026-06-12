namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion.Publica;

public class SubastaPublicaDetalleDto
{
    public int IdCotizacion { get; set; }
    public string NroCotizacion { get; set; }
    public string Tipo { get; set; }
    public string Titulo { get; set; }
    public string Estado { get; set; }
    public string UnidadAdm { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal? MejorOfertaActual { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Moneda { get; set; }
    public int CantOfertas { get; set; }
    public List<ItemPublicoDto> Items { get; set; } = new();

    public List<OfertaPublicaDto> HistorialOfertas { get; set; } = new();
}

public class OfertaPublicaDto
{
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
}