namespace PortalSubastas.Providers.Application.ResponseDto.Proveedor;

public class ProviderListResponseDto
{
    public List<ProviderListDto> Data { get; set; } = new();
    public int Total { get; set; }
}
