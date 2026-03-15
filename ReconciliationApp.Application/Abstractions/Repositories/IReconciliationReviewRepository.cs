using ReconciliationApp.Domain.Entities.ReconciliationReview;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IReconciliationReviewRepository
{
    Task<ReconciliationRun?> GetRunAsync(string runId, CancellationToken ct = default);
    Task<ReviewRunTotals?> GetRunTotalsAsync(string runId, CancellationToken ct = default);
    Task<string?> GetRunCompanyNameAsync(string runId, CancellationToken ct = default);

    Task<ReconciliationRun?> GetCurrentRunAsync(Guid companyId, CancellationToken ct = default);
    Task<ReviewRunTotals?> GetCurrentRunTotalsAsync(Guid companyId, CancellationToken ct = default);
    Task<string?> GetCompanyNameAsync(Guid companyId, CancellationToken ct = default);

    Task<bool> AcceptCaseAsync(string runId, string caseId, CancellationToken ct = default);
    Task<bool> MarkExceptionAsync(string runId, string caseId, CancellationToken ct = default);
    Task<int> BulkAcceptAsync(string runId, IEnumerable<string> caseIds, CancellationToken ct = default);
    Task<(bool CanConfirm, string Status)> ConfirmAsync(string runId, CancellationToken ct = default);
}