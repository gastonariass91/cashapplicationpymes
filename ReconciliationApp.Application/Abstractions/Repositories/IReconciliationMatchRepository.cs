using ReconciliationApp.Domain.Entities.Reconciliation;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IReconciliationMatchRepository
{
    Task<List<ReconciliationMatch>> ListByRunIdAsync(Guid batchRunId, CancellationToken ct = default);
    Task DeleteByRunIdAsync(Guid batchRunId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<ReconciliationMatch> matches, CancellationToken ct = default);
}
