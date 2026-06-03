namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

public class CotizacionDocumentoResponseDto
{
    public int IdCotDocumento { get; set; }
    public int IdCotizacion { get; set; }
    public string TipoDocumento { get; set; } = null!;
    public string NombreArchivo { get; set; } = null!;
    public string UrlArchivo { get; set; } = null!;
    public DateTime? FecIng { get; set; }
}
