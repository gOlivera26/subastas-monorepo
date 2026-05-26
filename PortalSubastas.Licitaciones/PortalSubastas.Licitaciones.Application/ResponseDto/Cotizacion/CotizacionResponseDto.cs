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
    public string Tipo { get; set; }
    public string Estado { get; set; }
    public string Modalidad { get; set; }

    public CotizacionEspecificacionResponseDto Especificacion { get; set; }
    public List<CotizacionDetalleResponseDto> Detalles { get; set; } = new();
    public List<CotizacionRenglonResponseDto> Renglones { get; set; } = new();
    public List<CotizacionProveedorResponseDto> Proveedores { get; set; } = new();
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
    public string? NItem { get; set; }
    public string? NroReserva { get; set; }
    public int? IdRenglon { get; set; }
    public decimal Cantidad { get; set; }
    public decimal ImporteBase { get; set; }
}

public class CotizacionRenglonResponseDto
{
    public int IdRenglon { get; set; }
    public int NumeroRenglon { get; set; }
    public string Descripcion { get; set; }
}

public class CotizacionProveedorResponseDto
{
    public int IdCotizacionProveedor { get; set; }
    public int IdProveedor { get; set; }
    public string Ganadora { get; set; }
}

public class SubastaDashboardDto
{
    public int IdCotizacion { get; set; }
    public int IdEstado { get; set; }
    public string NroCotizacion { get; set; }
    public string Tipo { get; set; }
    public string Estado { get; set; }
    public string Titulo { get; set; }
    public string UnidadAdm { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}
