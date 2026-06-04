namespace PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;

public class DocumentoItemRequestDto
{
    public int IdCotizacion { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public IFormFile Archivo { get; set; } = null!;
}
