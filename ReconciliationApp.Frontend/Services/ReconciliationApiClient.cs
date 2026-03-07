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

    public async Task<bool> AcceptCaseAsync(string runId, string caseId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/reconciliation-runs/{runId}/cases/{caseId}/accept", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MarkExceptionAsync(string runId, string caseId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/reconciliation-runs/{runId}/cases/{caseId}/exception", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> BulkAcceptAsync(string runId, IEnumerable<string> caseIds, CancellationToken ct = default)
    {
        var payload = new { caseIds = caseIds.ToList() };
        var response = await _http.PostAsJsonAsync($"api/reconciliation-runs/{runId}/cases/bulk-accept", payload, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, string? ErrorMessage)> ConfirmAsync(string runId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/reconciliation-runs/{runId}/confirm", null, ct);
        if (response.IsSuccessStatusCode) return (true, null);

        var content = await response.Content.ReadAsStringAsync(ct);
        return (false, string.IsNullOrWhiteSpace(content) ? "No se pudo confirmar." : content);
    }
}
