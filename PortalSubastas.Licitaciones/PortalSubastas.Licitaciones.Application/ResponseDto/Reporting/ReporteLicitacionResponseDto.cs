namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class ReporteLicitacionResponseDto
{
    public int IdCotizacion { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTimeOffset FechaEmision { get; set; }
    public List<ReporteLicitacionRenglonResponseDto> Renglones { get; set; } = new();
}

public sealed class ReporteLicitacionRenglonResponseDto
{
    public int Numero { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public decimal PrecioEstimado { get; set; }
}
