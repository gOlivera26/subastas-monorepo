namespace PortalSubastas.Contracts.Events;

public record SubastaDesistidaEvent(
    int IdCotizacion,
    string NroCotizacion,
    string Titulo,
    string Motivo,
    string TipoContratacion,
    List<ProveedorInfo> Proveedores,
    DateTime OccuredOn
);
