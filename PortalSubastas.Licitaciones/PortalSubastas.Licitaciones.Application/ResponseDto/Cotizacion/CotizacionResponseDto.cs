namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

public class CotizacionResponseDto
{
    public int IdCotizacion { get; set; }
    public string NroCotizacion { get; set; }
    public int IdEstado { get; set; }
    public int IdTipoContratacion { get; set; }
    public int IdVigencia { get; set; }
    public int IdOrganizacion { get; set; }
    public int IdUnidadAdm { get; set; }
    public string Observacion { get; set; }

    public CotizacionEspecificacionResponseDto Especificacion { get; set; }
    public List<CotizacionDetalleResponseDto> Detalles { get; set; } = new();
}

public class CotizacionEspecificacionResponseDto
{
    public int IdCotEspecificacion { get; set; }
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

public class CotizacionDetalleResponseDto
{
    public int IdCotizacionDetalle { get; set; }
    public int IdReservaDetalle { get; set; }
    public int IdItem { get; set; }
    public int? IdRenglon { get; set; }
    public decimal Cantidad { get; set; }
    public decimal ImporteBase { get; set; }
}

public class SubastaDashboardDto
{
    public int IdCotizacion { get; set; }
    public string NroCotizacion { get; set; }
    public string Tipo { get; set; } // Nombre del tipo ej "Subasta Inversa"
    public string Estado { get; set; } // Nombre del estado
    public string Titulo { get; set; } // Puede ser la observacion o nombre
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
