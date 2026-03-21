using System.Net.Http.Headers;
using System.Net.Http.Json;
using ReconciliationApp.Frontend.Contracts.Reconciliation;
using ReconciliationApp.Frontend.State;

namespace ReconciliationApp.Frontend.Services;

public sealed record CreateBatchRequestDto(Guid CompanyId, DateOnly PeriodFrom, DateOnly PeriodTo);
public sealed record CreateBatchResponseDto(Guid BatchId, Guid CompanyId, DateOnly PeriodFrom, DateOnly PeriodTo);
public sealed record CreateRunResponseDto(Guid BatchId, int RunNumber, DateTimeOffset CreatedAt);
public sealed record UploadCsvRequestDto(string Csv);

public sealed record ImportErrorDto(
    int RowNumber,
    string Message
);

public sealed record ImportResultDto(
    string ImportType,
    int ProcessedCount,
    int InsertedCount,
    int UpdatedCount,
    int IgnoredCount,
    int ClosedCount,
    int ErrorCount,
    IReadOnlyList<ImportErrorDto> Errors
);

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
    private readonly AuthState _auth;

    public ReconciliationApiClient(HttpClient http, AuthState auth)
    {
        _http = http;
        _auth = auth;
    }

    // Agrega el token JWT a cada request si el usuario está autenticado
    private void SetAuthHeader()
    {
        if (_auth.IsAuthenticated && _auth.Token is not null)
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _auth.Token);
        else
            _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<CreateBatchResponseDto?> CreateBatchAsync(
        Guid companyId,
        DateOnly periodFrom,
        DateOnly periodTo,
        CancellationToken ct = default)
    {
        SetAuthHeader();
        var payload = new CreateBatchRequestDto(companyId, periodFrom, periodTo);
        var response = await _http.PostAsJsonAsync("batches", payload, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CreateBatchResponseDto>(cancellationToken: ct);
    }

    public async Task<CreateRunResponseDto?> CreateRunAsync(Guid batchId, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.PostAsync($"batches/{batchId}/runs", null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CreateRunResponseDto>(cancellationToken: ct);
    }

    public async Task<ImportResultDto?> UploadCustomersCsvAsync(Guid batchId, int runNumber, string csv, CancellationToken ct = default)
    {
        SetAuthHeader();
        var payload = new UploadCsvRequestDto(csv);
        var response = await _http.PostAsJsonAsync($"batches/{batchId}/runs/{runNumber}/customers-csv", payload, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ImportResultDto>(cancellationToken: ct);
    }

    public async Task<ImportResultDto?> UploadDebtCsvAsync(Guid batchId, int runNumber, string csv, CancellationToken ct = default)
    {
        SetAuthHeader();
        var payload = new UploadCsvRequestDto(csv);
        var response = await _http.PostAsJsonAsync($"batches/{batchId}/runs/{runNumber}/debt-csv", payload, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ImportResultDto>(cancellationToken: ct);
    }

    public async Task<ImportResultDto?> UploadPaymentsCsvAsync(Guid batchId, int runNumber, string csv, CancellationToken ct = default)
    {
        SetAuthHeader();
        var payload = new UploadCsvRequestDto(csv);
        var response = await _http.PostAsJsonAsync($"batches/{batchId}/runs/{runNumber}/payments-csv", payload, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ImportResultDto>(cancellationToken: ct);
    }

    public async Task<ReconcileRunResponseDto?> ReconcileAsync(Guid batchId, int runNumber, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.PostAsync($"batches/{batchId}/runs/{runNumber}/reconcile", null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReconcileRunResponseDto>(cancellationToken: ct);
    }

    public async Task<ApiReconciliationRunDto?> GetRunAsync(string runId, CancellationToken ct = default)
    {
        SetAuthHeader();
        return await _http.GetFromJsonAsync<ApiReconciliationRunDto>(
            $"api/reconciliation-runs/{runId}", cancellationToken: ct);
    }

    public async Task<ApiReconciliationRunDto?> GetCurrentRunAsync(Guid companyId, CancellationToken ct = default)
    {
        SetAuthHeader();
        return await _http.GetFromJsonAsync<ApiReconciliationRunDto>(
            $"api/companies/{companyId}/reconciliation/current", cancellationToken: ct);
    }

    public async Task<bool> AcceptCaseAsync(string runId, string caseId, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.PostAsync($"api/reconciliation-runs/{runId}/cases/{caseId}/accept", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MarkExceptionAsync(string runId, string caseId, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.PostAsync($"api/reconciliation-runs/{runId}/cases/{caseId}/exception", null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> BulkAcceptAsync(string runId, IEnumerable<string> caseIds, CancellationToken ct = default)
    {
        SetAuthHeader();
        var payload = new { caseIds = caseIds.ToList() };
        var response = await _http.PostAsJsonAsync($"api/reconciliation-runs/{runId}/cases/bulk-accept", payload, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, string? ErrorMessage)> ConfirmAsync(string runId, CancellationToken ct = default)
    {
        SetAuthHeader();
        var response = await _http.PostAsync($"api/reconciliation-runs/{runId}/confirm", null, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        var content = await response.Content.ReadAsStringAsync(ct);
        return (false, string.IsNullOrWhiteSpace(content) ? "No se pudo confirmar." : content);
    }
}
