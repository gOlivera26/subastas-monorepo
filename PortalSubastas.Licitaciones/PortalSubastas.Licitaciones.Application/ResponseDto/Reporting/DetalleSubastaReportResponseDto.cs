namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class DetalleSubastaReportResponseDto
{
    public DetalleSubastaCabeceraResponseDto Cabecera { get; set; } = new();
    public List<DetalleSubastaItemResponseDto> Items { get; set; } = new();
    public List<DetalleSubastaProveedorResponseDto> Proveedores { get; set; } = new();
}

public sealed class DetalleSubastaCabeceraResponseDto
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

public sealed class DetalleSubastaItemResponseDto
{
    public int IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public int Numero { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal ImporteBase { get; set; }
    public decimal TotalBase { get; set; }
    public decimal? ImporteMinimo { get; set; }
    public string? Moneda { get; set; }
}

public sealed class DetalleSubastaProveedorResponseDto
{
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public string EstadoParticipacion { get; set; } = string.Empty;
}
