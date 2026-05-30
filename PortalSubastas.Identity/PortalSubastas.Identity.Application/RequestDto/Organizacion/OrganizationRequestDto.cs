namespace PortalSubastas.Identity.Application.RequestDto.Organizacion;

public class OrganizationRequestDto
{
    public string Nombre { get; set; }
    public string Cuit { get; set; }
    public string Abreviatura { get; set; }
    public bool? Activo { get; set; }
}
