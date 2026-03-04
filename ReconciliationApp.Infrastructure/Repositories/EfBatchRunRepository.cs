using ReconciliationApp.Application.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;
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

    public Task<ReconciliationApp.Domain.Entities.Batching.BatchRun?> GetByBatchAndRunNumberAsync(Guid batchId, int runNumber, CancellationToken ct = default)
        => _db.BatchRuns.SingleOrDefaultAsync(r => r.BatchId == batchId && r.RunNumber == runNumber, ct);
}
