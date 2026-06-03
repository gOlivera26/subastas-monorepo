namespace PortalSubastas.Licitaciones.Application.ResponseDto.OfertaSubasta;

public class OfertaItemResponseDto
{
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public string? TextoError { get; set; }
    public bool Prorrogada { get; set; }
    public DateTime? FechaFinProrroga { get; set; }
    public int? IdOfertaSubasta { get; set; }
    public decimal? Monto { get; set; }
    public int? IdProveedor { get; set; }
    public DateTime? FechaOferta { get; set; }
}