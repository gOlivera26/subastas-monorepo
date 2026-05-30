namespace PortalSubastas.Licitaciones.Application.RequestDto.Garantia;

public class GarantiaRequestDto
{
    public int IdCotizacion { get; set; }
    public int IdProveedor { get; set; }
    public int IdTipoDocumento { get; set; }
    public int IdMoneda { get; set; }
    public string? CompaniaAseguradora { get; set; }
    public string? NroPoliza { get; set; }
    public decimal? MontoCaucion { get; set; }
    public decimal? MontoPagare { get; set; }
    public DateTime? FechaPagare { get; set; }
    public string? Observacion { get; set; }
    public IFormFile Archivo { get; set; } = null!;
}
