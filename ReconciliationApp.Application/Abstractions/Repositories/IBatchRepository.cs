using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IBatchRepository
{
    void Add(ReconciliationBatch batch);
    Task<ReconciliationBatch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsForPeriodAsync(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
