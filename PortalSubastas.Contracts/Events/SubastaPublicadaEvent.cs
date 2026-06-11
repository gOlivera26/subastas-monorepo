namespace PortalSubastas.Contracts.Events;

public record ProveedorInfo(
    int IdProveedor,
    string EmailProveedor,
    string NombreProveedor
);

public record SubastaPublicadaEvent(
    int IdCotizacion,
    string NroCotizacion,
    string Titulo,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    string TipoContratacion,
    List<ProveedorInfo> Proveedores,
    DateTime OccuredOn
);
