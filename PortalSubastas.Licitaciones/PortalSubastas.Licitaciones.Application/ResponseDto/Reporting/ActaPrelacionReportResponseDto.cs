namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class ActaPrelacionReportResponseDto
{
    public ActaPrelacionCabeceraResponseDto Cabecera { get; set; } = new();
    public List<ActaPrelacionDetalleResponseDto> Detalles { get; set; } = new();
    public List<ActaPrelacionOfertaResponseDto> OfertasIniciales { get; set; } = new();
    public List<ActaPrelacionOfertaResponseDto> HistorialOfertas { get; set; } = new();
    public List<ActaPrelacionGanadorResponseDto> Ganadores { get; set; } = new();
}

public sealed class ActaPrelacionCabeceraResponseDto
{
    public int IdCotizacion { get; set; }
    public string NumeroCotizacion { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoContratacion { get; set; } = string.Empty;
    public string CriterioAdjudicacion { get; set; } = string.Empty;
    public string UnidadAdministrativa { get; set; } = string.Empty;
    public string? NumeroExpediente { get; set; }
    public decimal? MargenMejora { get; set; }
    public DateTime? FechaInicioSubasta { get; set; }
    public DateTime? FechaFinalizacionSubasta { get; set; }
    public DateTimeOffset FechaEmision { get; set; }
}

public sealed class ActaPrelacionDetalleResponseDto
{
    public int IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int Numero { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal TotalBase { get; set; }
}

public sealed class ActaPrelacionOfertaResponseDto
{
    public int IdOfertaSubasta { get; set; }
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int NumeroDetalle { get; set; }
    public string Detalle { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal Monto { get; set; }
    public decimal Total { get; set; }
    public DateTime FechaOferta { get; set; }
    public int NumeroOferta { get; set; }
    public bool EsOfertaInicial { get; set; }
}

public sealed class ActaPrelacionGanadorResponseDto
{
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int NumeroDetalle { get; set; }
    public string Detalle { get; set; } = string.Empty;
    public decimal MontoGanador { get; set; }
    public decimal CantidadAdjudicada { get; set; }
}
