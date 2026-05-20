namespace PortalSubastas.Providers.Application.RequestDto.Proveedor;

public class UpdateDomicilioDto
{
    public int Id { get; set; }
    public int IdTipoDomicilio { get; set; }
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Piso { get; set; }
    public string? Departamento { get; set; }
    public string Barrio { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public int IdProvincia { get; set; }
    public string CodigoPostal { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Fax { get; set; }
}
