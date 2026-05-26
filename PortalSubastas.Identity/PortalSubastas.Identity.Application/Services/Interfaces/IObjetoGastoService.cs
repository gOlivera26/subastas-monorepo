using PortalSubastas.Identity.Application.RequestDto.ObjetoGasto;
using PortalSubastas.Identity.Application.ResponseDto.ObjetoGasto;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IObjetoGastoService
{
    Task<OperationResponse<List<ObjetoGastoResponseDto>>> GetAllAsync(int? idVigencia);
    Task<OperationResponse<ObjetoGastoResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<ObjetoGastoResponseDto>> CreateAsync(ObjetoGastoRequestDto dto);
    Task<OperationResponse<ObjetoGastoResponseDto>> UpdateAsync(int id, ObjetoGastoRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
    Task<OperationResponse<int>> UploadCsvAsync(ObjetoGastoBulkUploadDto bulk);
}
