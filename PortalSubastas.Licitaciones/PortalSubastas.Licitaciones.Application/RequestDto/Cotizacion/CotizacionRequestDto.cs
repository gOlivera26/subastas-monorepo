namespace PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;

public class CotizacionRequestDto
{
    public int IdTipoContratacion { get; set; } // 7 = Subasta, 9 = Subasta Directa
    public int IdVigencia { get; set; }
    public int IdOrganizacion { get; set; }
    public int IdUnidadAdm { get; set; }
    public string Observacion { get; set; }

    public CotizacionEspecificacionRequestDto Especificacion { get; set; }
    public List<CotizacionDetalleRequestDto> Detalles { get; set; } = new();
}

public class CotizacionEspecificacionRequestDto
{
    public string NroExpediente { get; set; }
    public DateTime? FechaInicioSubasta { get; set; }
    public DateTime? FechaFinalizacionSubasta { get; set; }
    public DateTime? FechaLimiteConsultas { get; set; }
    public decimal? MargenMejora { get; set; }
    public int? CriterioAdjudicacion { get; set; }
    public bool PermiteProrroga { get; set; }
    public int? ProrrogaMinutos { get; set; }
    public string Redeterminacion { get; set; }
}

public class CotizacionDetalleRequestDto
{
    public int IdReservaDetalle { get; set; }
    public int IdItem { get; set; }
    public int? IdRenglon { get; set; }
    public decimal Cantidad { get; set; }
    public decimal ImporteBase { get; set; }
}
