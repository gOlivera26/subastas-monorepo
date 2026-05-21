namespace PortalSubastas.Identity.Application.ResponseDto.UnidadAdministrativa;

public class UnidadAdministrativaResponseDto
{
    public int IdUnidadAdm { get; set; }
    public int NumeroUnidadAdm { get; set; }
    public string NombreUnidadAdm { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public string OrganizacionNombre { get; set; }
    public string Mail { get; set; }
    public string Alias { get; set; }
    public short? Puerto { get; set; }
    public string Smtp { get; set; }
    public string VigenciaEjercicio { get; set; }
}
