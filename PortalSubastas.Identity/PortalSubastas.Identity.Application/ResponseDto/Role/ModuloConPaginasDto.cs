namespace PortalSubastas.Identity.Application.ResponseDto.Role;

public class ModuloConPaginasDto
{
    public int IdModulo { get; set; }
    public string ModuloTitulo { get; set; }
    public List<PaginaDto> Paginas { get; set; } = new();
}
