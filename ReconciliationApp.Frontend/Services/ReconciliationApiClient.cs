using System.Net.Http.Json;
using ReconciliationApp.Frontend.Contracts.Reconciliation;

namespace ReconciliationApp.Frontend.Services;

public sealed record CreateBatchRequestDto(Guid CompanyId, DateOnly PeriodFrom, DateOnly PeriodTo);
public sealed record CreateBatchResponseDto(Guid BatchId, Guid CompanyId, DateOnly PeriodFrom, DateOnly PeriodTo);
public sealed record CreateRunResponseDto(Guid BatchId, int RunNumber, DateTimeOffset CreatedAt);
public sealed record UploadCsvRequestDto(string Csv);

public sealed record ReconcileMatchResponseDto(
    int DebtRowNumber,
    int PaymentRowNumber,
    string CustomerId,
    decimal Amount
);

public sealed record ReconcileRunResponseDto(
    Guid BatchRunId,
    DateTimeOffset? ReconciledAt,
    int MatchesSaved,
    IReadOnlyList<ReconcileMatchResponseDto> Matches,
    IReadOnlyList<int> UnmatchedDebtRowNumbers,
    IReadOnlyList<int> UnmatchedPaymentRowNumbers,
    bool AlreadyReconciled
);

public sealed class ReconciliationApiClient
{
    private readonly HttpClient _http;

    public ReconciliationApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<CreateBatchResponseDto?> CreateBatchAsync(
        Guid companyId,
        DateOnly periodFrom,
        DateOnly periodTo,
        CancellationToken ct = default)
    {
        var payload = new CreateBatchRequestDto(companyId, periodFrom, periodTo);

        var response = await _http.PostAsJsonAsync("batches", payload, ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<CreateBatchResponseDto>(cancellationToken: ct);
    }

    public async Task<CreateRunResponseDto?> CreateRunAsync(Guid batchId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"batches/{batchId}/runs", null, ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(cancellationToken: ct);
    }

    public async Task<bool> UploadCustomersCsvAsync(Guid batchId, int runNumber, string csv, CancellationToken ct = default)
    {
        var payload = new UploadCsvRequestDto(csv);
        var response = await _http.PostAsJsonAsync($"batches/{batchId}/runs/{runNumber}/customers-csv", payload, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UploadDebtCsvAsync(Guid batchId, int runNumber, string csv, CancellationToken ct = default)
    {
        var payload = new UploadCsvRequestDto(csv);
        var response = await _http.PostAsJsonAsync($"batches/{batchId}/runs/{runNumber}/debt-csv", payload, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UploadPaymentsCsvAsync(Guid batchId, int runNumber, string csv, CancellationToken ct = default)
    {
        var payload = new UploadCsvRequestDto(csv);
        var response = await _http.PostAsJsonAsync($"batches/{batchId}/runs/{runNumber}/payments-csv", payload, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<ReconcileRunResponseDto?> ReconcileAsync(Guid batchId, int runNumber, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"batches/{batchId}/runs/{runNumber}/reconcile", null, ct);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<ReconcileRunResponseDto>(cancellationToken: ct);
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