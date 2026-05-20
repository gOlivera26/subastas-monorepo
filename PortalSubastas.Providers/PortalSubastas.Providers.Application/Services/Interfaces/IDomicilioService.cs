namespace PortalSubastas.Providers.Application.Services.Interfaces;

public interface IDomicilioService
{
    Task<OperationResponse<List<DomicilioDto>>> GetDomiciliosByPersonaAsync(int personaId);
    Task<OperationResponse<DomicilioDto>> CreateAsync(int personaId, CreateDomicilioDto dto);
    Task<OperationResponse<DomicilioDto>> UpdateAsync(UpdateDomicilioDto dto);
    Task<OperationResponse<bool>> DeleteAsync(int domicilioId);
}
