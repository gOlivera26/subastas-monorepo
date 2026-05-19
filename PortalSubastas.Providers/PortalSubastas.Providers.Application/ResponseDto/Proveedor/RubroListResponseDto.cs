namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class RubroListResponseDto
{
    public List<RubroListDto> Data { get; set; } = new();
    public int Total { get; set; }
}
