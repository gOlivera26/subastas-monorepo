using PortalSubastas.Identity.Application.RequestDto.Login;
using PortalSubastas.Identity.Application.ResponseDto.Perfil;

namespace PortalSubastas.Identity.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<OperationResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
        Task<OperationResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task<OperationResponse<ProfileResponseDto>> UpdateProfileAsync(UpdateProfileRequestDto request);
        Task<OperationResponse<ProfileResponseDto>> GetProfileAsync();
        Task<OperationResponse<bool>> ChangePasswordAsync(ChangePasswordRequestDto request);
    }
}
