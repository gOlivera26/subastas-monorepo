namespace PortalSubastas.Licitaciones.Application.ResponseDto.Catalogos;

public class CatalogoBienResponseDto
{
    public int IdItem { get; set; }
    public string Codigo { get; set; } = null!;
    public string NItem { get; set; } = null!;
    public bool Vigente => FecBaja == null;
    public DateTime? FecBaja { get; set; }
    public int? NumeroJurisdiccion { get; set; }
    public int? IdCategoriaBien { get; set; }
}
