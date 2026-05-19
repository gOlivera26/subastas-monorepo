namespace PortalSubastas.Providers.Application.Services.Interfaces;

public interface IAfipService
{
    Task<OperationResponse<AfipPersonDataDto>> GetPersonDataAsync(string cuit);
}
