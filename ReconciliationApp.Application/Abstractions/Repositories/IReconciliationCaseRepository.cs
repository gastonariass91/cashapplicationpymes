using ReconciliationApp.Domain.Entities.ReconciliationReview;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IReconciliationCaseRepository
{
    Task<ReconciliationRun?> GetByBatchRunIdAsync(Guid batchRunId, CancellationToken ct = default);
    Task DeleteByBatchRunIdAsync(Guid batchRunId, CancellationToken ct = default);
    Task AddRunAsync(ReconciliationRun run, CancellationToken ct = default);
    Task AddCasesAsync(IEnumerable<ReconciliationCase> cases, CancellationToken ct = default);
}
