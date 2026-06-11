using PortalSubastas.Licitaciones.Application.RequestDto.Cotizacion;
using PortalSubastas.Licitaciones.Application.ResponseDto.Cotizacion;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IDocumentoItemService
{
    Task<OperationResponse<List<DocumentoItemResponseDto>>> GetByItemAsync(int idCotizacion, int? idCotizacionDetalle, int? idRenglon);
    Task<OperationResponse<DocumentoItemResponseDto>> UploadAsync(DocumentoItemRequestDto request);
    Task<OperationResponse<bool>> DeleteAsync(int idDocItem);
    Task<OperationResponse<bool>> EnviarDocumentacionDefinitivaAsync(int idCotizacion, int? idCotizacionDetalle, int? idRenglon);
}
