namespace PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

public class SubastaDetalleReducidoDto
{
    public string Numero { get; set; } = string.Empty;
    public string Expediente { get; set; } = string.Empty;
    public string Objeto { get; set; } = string.Empty;
    public string TipoContratacion { get; set; } = string.Empty;
    public string Modalidad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public DateTime? FechaLimiteConsultas { get; set; }
    public decimal? MargenMejora { get; set; }
    public List<SubastaItemReducidoDto> Items { get; set; } = new();
}

public class SubastaItemReducidoDto
{
    public int IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public string NItem { get; set; } = string.Empty;
    public int? IdMoneda { get; set; }
    public decimal Cantidad { get; set; }
    public decimal ImporteBase { get; set; }
    public decimal? ImporteMinimo { get; set; }
    public decimal? MejorOfertaActual { get; set; }
}

public class SubastaResumenOfertasDto
{
    public int CantidadOfertas { get; set; }
    public decimal? MejorOferta { get; set; }
    public string Estado { get; set; } = string.Empty;
}
