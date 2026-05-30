namespace PortalSubastas.Identity.Application.RequestDto.ObjetoGasto;

public class ObjetoGastoRequestDto
{
    public int? IdObjetoGastoRel { get; set; }
    public string NumeroObjeto { get; set; }
    public string NombreObjeto { get; set; }
    public int IdVigencia { get; set; }
    public int? IdOrganizacion { get; set; }
    public bool? ImputaEjecucion { get; set; }
}

public class ObjetoGastoBulkItemDto
{
    public int IdObjetoGasto { get; set; }
    public int? IdObjetoGastoRel { get; set; }
    public string NumeroObjeto { get; set; }
    public string NombreObjeto { get; set; }
    public bool? ImputaEjecucion { get; set; }
}

public class ObjetoGastoBulkUploadDto
{
    public List<ObjetoGastoBulkItemDto> Items { get; set; } = new();
    public int? IdOrganizacion { get; set; }
}
