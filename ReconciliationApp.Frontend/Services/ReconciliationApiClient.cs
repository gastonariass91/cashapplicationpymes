using System.Net.Http.Json;
using ReconciliationApp.Frontend.Contracts.Reconciliation;

namespace ReconciliationApp.Frontend.Services;

public sealed class ReconciliationApiClient
{
    private readonly HttpClient _http;

    public ReconciliationApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiReconciliationRunDto?> GetRunAsync(string runId, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<ApiReconciliationRunDto>(
            $"api/reconciliation-runs/{runId}",
            cancellationToken: ct
        );
    }
}
