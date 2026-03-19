using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfBatchRunRepository : IBatchRunRepository
{
    private readonly AppDbContext _db;
    public EfBatchRunRepository(AppDbContext db) => _db = db;

    public Task AddAsync(BatchRun run, CancellationToken ct)
    {
        _db.BatchRuns.Add(run);
        return Task.CompletedTask;
    }

    public Task<BatchRun?> GetByBatchAndRunNumberAsync(Guid batchId, int runNumber, CancellationToken ct = default)
        => _db.BatchRuns.SingleOrDefaultAsync(r => r.BatchId == batchId && r.RunNumber == runNumber, ct);

    /// <summary>
    /// Devuelve el run con el RunNumber más alto del batch.
    /// En el modelo online esto es siempre el run activo.
    /// </summary>
    public Task<BatchRun?> GetCurrentByBatchIdAsync(Guid batchId, CancellationToken ct = default)
        => _db.BatchRuns
              .Where(r => r.BatchId == batchId)
              .OrderByDescending(r => r.RunNumber)
              .FirstOrDefaultAsync(ct);
}
