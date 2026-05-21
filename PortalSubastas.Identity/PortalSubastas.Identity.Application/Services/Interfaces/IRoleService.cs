using PortalSubastas.Identity.Application.RequestDto.Role;
using PortalSubastas.Identity.Application.ResponseDto.Role;

namespace PortalSubastas.Identity.Application.Services.Interfaces;

public interface IRoleService
{
    Task<OperationResponse<List<RoleResponseDto>>> GetAllAsync();
    Task<OperationResponse<List<RoleResponseDto>>> GetActiveRolesAsync();
    Task<OperationResponse<RoleResponseDto>> GetByIdAsync(int id);
    Task<OperationResponse<RoleResponseDto>> CreateAsync(RoleRequestDto dto);
    Task<OperationResponse<RoleResponseDto>> UpdateAsync(int id, RoleRequestDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int id);
    Task<OperationResponse<List<RoleModuleResponseDto>>> GetModulesByRoleAsync(int idRol);
    Task<OperationResponse<RoleModuleResponseDto>> AssignModuleAsync(RoleModuleRequestDto dto);
    Task<OperationResponse<bool>> UnassignModuleAsync(int idRol, int idModulo);
    Task<OperationResponse<List<ModuloDto>>> GetAllModulesAsync();
    Task<OperationResponse<List<PaginaDto>>> GetAllPagesAsync();
    Task<OperationResponse<List<PaginaDto>>> GetPagesByRoleAsync(int idRol);
    Task<OperationResponse<PaginaDto>> AssignPageAsync(int idRol, int idPagina);
    Task<OperationResponse<bool>> UnassignPageAsync(int idRol, int idPagina);
    Task<OperationResponse<List<ModuloConPaginasDto>>> GetModulosConPaginasAsync();
}
