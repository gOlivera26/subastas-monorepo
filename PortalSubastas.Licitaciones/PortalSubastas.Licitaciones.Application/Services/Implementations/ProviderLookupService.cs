using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using PortalSubastas.Licitaciones.Application.ResponseDto.Reporting;
using PortalSubastas.Licitaciones.Application.Services.Interfaces;

namespace PortalSubastas.Licitaciones.Application.Services.Implementations;

public sealed class ProviderLookupService : IProviderLookupService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProviderLookupService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyDictionary<int, ProviderReportLookupDto>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var normalizedIds = ids
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
            return new Dictionary<int, ProviderReportLookupDto>();

        PropagateAuthorizationHeader();

        var query = string.Join("&", normalizedIds.Select(id => $"ids={id}"));
        var response = await _httpClient.GetAsync($"api/Provider/by-ids?{query}", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new Dictionary<int, ProviderReportLookupDto>();

        var operation = await response.Content.ReadFromJsonAsync<ProviderOperationResponse<List<ProviderReportLookupDto>>>(
            cancellationToken: cancellationToken);

        if (operation?.Success != true || operation.Data is null)
            return new Dictionary<int, ProviderReportLookupDto>();

        return operation.Data
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private void PropagateAuthorizationHeader()
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrWhiteSpace(authorization) &&
            AuthenticationHeaderValue.TryParse(authorization, out var headerValue))
        {
            _httpClient.DefaultRequestHeaders.Authorization = headerValue;
        }
    }

    private sealed class ProviderOperationResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int? Code { get; set; }
    }
}
