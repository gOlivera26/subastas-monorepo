using PortalSubastas.Identity.Application.RequestDto.CategoriaProgramatica;
using PortalSubastas.Identity.Application.ResponseDto.CategoriaProgramatica;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface ICategoriaProgramaticaService
{
    Task<OperationResponse<List<CategoriaProgramaticaResponseDto>>> GetAllAsync(int? idVigencia);
    Task<OperationResponse<CategoriaProgramaticaResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<CategoriaProgramaticaResponseDto>> CreateAsync(CategoriaProgramaticaRequestDto dto);
    Task<OperationResponse<CategoriaProgramaticaResponseDto>> UpdateAsync(int id, CategoriaProgramaticaRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
    Task<OperationResponse<int>> UploadCsvAsync(CategoriaProgramaticaBulkUploadDto bulk);
}
