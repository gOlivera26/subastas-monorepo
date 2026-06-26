namespace PortalSubastas.Licitaciones.Application.ResponseDto.Tablero;

public sealed class TableroResponseDto
{
    public int? IdVigencia { get; set; }
    public short? Ejercicio { get; set; }
    public int CriterioAdjudicacion { get; set; }
    public string Criterio { get; set; } = string.Empty;
    public decimal TotalPresupuestado { get; set; }
    public decimal TotalSubastado { get; set; }
    public decimal TotalAhorrado { get; set; }
    public decimal PorcentajeAhorro { get; set; }
    public int CantidadCotizaciones { get; set; }
    public int CantidadFilas { get; set; }
    public List<TableroItemDto> Items { get; set; } = new();
}

public sealed class TableroItemDto
{
    public int IdCotizacion { get; set; }
    public string Cotizacion { get; set; } = string.Empty;
    public int IdProveedor { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public int? IdCotizacionDetalle { get; set; }
    public int? IdRenglon { get; set; }
    public string Item { get; set; } = string.Empty;
    public decimal Presupuestado { get; set; }
    public decimal Subastado { get; set; }
    public decimal Ahorrado { get; set; }
    public decimal PorcentajeAhorro { get; set; }
}
