namespace PortalSubastas.Identity.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<OperationResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
        Task<OperationResponse<LoginResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task<OperationResponse<ProfileResponseDto>> UpdateProfileAsync(UpdateProfileRequestDto request);
        Task<OperationResponse<ProfileResponseDto>> GetProfileAsync();
        Task<OperationResponse<bool>> ChangePasswordAsync(ChangePasswordRequestDto request);
        Task<OperationResponse<LoginResponseDto>> SwitchContextAsync(SwitchContextRequestDto request);
        Task<OperationResponse<bool>> ConfirmarEmailAsync(ConfirmEmailRequestDto request);
        Task<OperationResponse<bool>> ReenviarCodigoAsync(string email);
        Task<OperationResponse<bool>> SolicitarResetPasswordAsync(SolicitarResetRequestDto request);
        Task<OperationResponse<bool>> ResetPasswordAsync(ResetPasswordRequestDto request);
    }
}
