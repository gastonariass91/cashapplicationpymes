using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IBatchRunRepository
{
    Task<BatchRun?> GetByBatchAndRunNumberAsync(Guid batchId, int runNumber, CancellationToken ct = default);

    /// <summary>
    /// Devuelve el run con el número más alto del batch (el run activo actual).
    /// Usado para que CreateRun sea idempotente en el modelo online.
    /// </summary>
    Task<BatchRun?> GetCurrentByBatchIdAsync(Guid batchId, CancellationToken ct = default);

    Task AddAsync(BatchRun run, CancellationToken ct);
}
