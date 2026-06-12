namespace PortalSubastas.Contracts.Events;

public record SubastaProrrogadaEvent(
    int IdCotizacion,
    string NroCotizacion,
    string Titulo,
    DateTime? FechaFinOriginal,
    DateTime? FechaFinNueva,
    int MinutosAgregados,
    string TipoContratacion,
    List<ProveedorInfo> Proveedores,
    DateTime OccuredOn
);
