namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion.Publica;

public class SubastaPublicaDetalleDto
{
    public int IdCotizacion { get; set; }
    public string NroCotizacion { get; set; }
    public string Tipo { get; set; }
    public string Titulo { get; set; }
    public string UnidadAdm { get; set; }
    public DateTime? FechaFin { get; set; }
    public List<ItemPublicoDto> Items { get; set; } = new();
}
