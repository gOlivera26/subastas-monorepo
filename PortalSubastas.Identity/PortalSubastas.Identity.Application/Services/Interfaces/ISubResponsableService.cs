using PortalSubastas.Identity.Application.RequestDto.SubResponsable;
using PortalSubastas.Identity.Application.ResponseDto.SubResponsable;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface ISubResponsableService
{
    Task<OperationResponse<List<SubResponsableResponseDto>>> GetAllAsync(int? idUnidadAdm);
    Task<OperationResponse<SubResponsableResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<SubResponsableResponseDto>> CreateAsync(SubResponsableRequestDto dto);
    Task<OperationResponse<SubResponsableResponseDto>> UpdateAsync(int id, SubResponsableRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
    Task<OperationResponse<int>> UploadCsvAsync(SubResponsableBulkUploadDto bulk);
}
