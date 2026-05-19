namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class DomicilioDto
{
    public int Id { get; set; }
    public int IdPersona { get; set; }
    public int IdTipoDomicilio { get; set; }
    public string TipoDomicilio { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Piso { get; set; }
    public string? Departamento { get; set; }
    public string Barrio { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public int IdProvincia { get; set; }
    public string Provincia { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Fax { get; set; }
}
