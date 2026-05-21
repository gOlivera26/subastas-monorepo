namespace PortalSubastas.Identity.Application.ResponseDto.Organizacion;

public class OrganizationResponseDto
{
    public int IdOrganizacion { get; set; }
    public string Nombre { get; set; }
    public string Cuit { get; set; }
    public string Abreviatura { get; set; }
    public bool? Activo { get; set; }
    public DateTime? FecCreacion { get; set; }
}
