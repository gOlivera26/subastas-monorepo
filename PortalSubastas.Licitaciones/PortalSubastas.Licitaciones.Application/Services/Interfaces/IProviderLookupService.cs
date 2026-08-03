using PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;

namespace PortalSubastas.Licitaciones.Application.Services.Interfaces;

public interface IProviderLookupService
{
    Task<IReadOnlyDictionary<int, ProviderReportLookupDto>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default);
}
