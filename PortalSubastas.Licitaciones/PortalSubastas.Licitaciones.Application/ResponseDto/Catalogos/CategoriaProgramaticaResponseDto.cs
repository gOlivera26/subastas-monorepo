namespace PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;

public class CategoriaProgramaticaResponseDto
{
    public int IdCatProg { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string CodigoCompleto { get; set; } = null!;
    public string? Naturaleza { get; set; }
    public int IdVigencia { get; set; }
}
