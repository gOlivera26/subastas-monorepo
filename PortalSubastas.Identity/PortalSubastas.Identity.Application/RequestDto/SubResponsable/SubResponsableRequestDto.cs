namespace PortalSubastas.Identity.Application.RequestDto.SubResponsable;

public class SubResponsableRequestDto
{
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public int? IdSubRespRel { get; set; }
    public bool Vigente { get; set; }
    public int? IdUnidadAdm { get; set; }
}

public class SubResponsableBulkItemDto
{
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public int? IdUnidadAdm { get; set; }
    public string NombreUnidadAdm { get; set; }
}

public class SubResponsableBulkUploadDto
{
    public List<SubResponsableBulkItemDto> Items { get; set; } = new();
}
