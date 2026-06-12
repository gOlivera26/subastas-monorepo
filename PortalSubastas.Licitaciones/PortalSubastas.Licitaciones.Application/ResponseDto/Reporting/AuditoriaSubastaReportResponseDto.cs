namespace PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

public sealed class AuditoriaSubastaReportResponseDto
{
    public AuditoriaSubastaCabeceraResponseDto Cabecera { get; set; } = new();
    public AuditoriaSubastaResumenResponseDto Resumen { get; set; } = new();
    public List<AuditoriaSubastaMovimientoResponseDto> Movimientos { get; set; } = new();
}

public sealed class AuditoriaSubastaCabeceraResponseDto
{
    public int IdCotizacion { get; set; }
    public string NumeroCotizacion { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoContratacion { get; set; } = string.Empty;
    public string CriterioAdjudicacion { get; set; } = string.Empty;
    public string UnidadAdministrativa { get; set; } = string.Empty;
    public string? NumeroExpediente { get; set; }
    public DateTime? FechaCotizacion { get; set; }
    public DateTime? FechaInicioSubasta { get; set; }
    public DateTimeOffset FechaEmision { get; set; }
}

public sealed class AuditoriaSubastaResumenResponseDto
{
    public int CantidadDocumentos { get; set; }
    public int CantidadOfertas { get; set; }
    public int CantidadProveedores { get; set; }
}

public sealed class AuditoriaSubastaMovimientoResponseDto
{
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string? Usuario { get; set; }
    public DateTime? Fecha { get; set; }
}
