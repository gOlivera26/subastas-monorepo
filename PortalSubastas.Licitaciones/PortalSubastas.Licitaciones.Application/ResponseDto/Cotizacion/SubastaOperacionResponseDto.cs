namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

public class SubastaOperacionResponseDto
{
    public string NroCotizacion { get; set; } = string.Empty;
    public int IdEstado { get; set; }
    public string Estado { get; set; } = string.Empty;
}
