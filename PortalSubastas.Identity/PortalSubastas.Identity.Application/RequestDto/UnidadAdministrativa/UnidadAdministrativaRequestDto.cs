namespace PortalSubastas.Identity.Application.RequestDto.UnidadAdministrativa;

public class UnidadAdministrativaRequestDto
{
    public int NumeroUnidadAdm { get; set; }
    public string NombreUnidadAdm { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public int? NroServicioAdm { get; set; }
    public int? IdProveedor { get; set; }
    public string Mail { get; set; }
    public string Alias { get; set; }
    public short? Puerto { get; set; }
    public string Smtp { get; set; }
}
