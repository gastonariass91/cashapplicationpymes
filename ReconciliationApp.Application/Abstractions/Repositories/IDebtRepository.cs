using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IDebtRepository
{
    Task<List<Debt>> ListByCompanyAsync(Guid companyId, CancellationToken ct);
    Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Debt> debts, CancellationToken ct);
}