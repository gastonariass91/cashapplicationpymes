using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IDebtRepository
{
    Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Debt> debts, CancellationToken ct);
}
