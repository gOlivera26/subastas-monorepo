using PortalSubastas.Licitaciones.Application.RequestDto.OfertaSubasta;
using PortalSubastas.Licitaciones.Application.ResponseDto.OfertaSubasta;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IOfertaSubastaService
{
    Task<OperationResponse<List<OfertaItemResponseDto>>> ProcesarOfertasAsync(int idCotizacion, List<OfertaItemRequestDto> ofertas);
    Task<OperationResponse<object>> GetHistorialAsync(int idCotizacion);
    Task<OperationResponse<object>> GetMisOfertasAsync();
}
