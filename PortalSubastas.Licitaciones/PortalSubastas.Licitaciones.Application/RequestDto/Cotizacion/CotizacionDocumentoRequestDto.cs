namespace PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;

public class CotizacionDocumentoRequestDto
{
    public int IdCotizacion { get; set; }
    public string TipoDocumento { get; set; } = null!;
    public IFormFile Archivo { get; set; } = null!;
}
