using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IBatchRunRepository
{
    Task<ReconciliationApp.Domain.Entities.Batching.BatchRun?> GetByBatchAndRunNumberAsync(Guid batchId, int runNumber, CancellationToken ct = default);

    Task AddAsync(BatchRun run, CancellationToken ct);
}
