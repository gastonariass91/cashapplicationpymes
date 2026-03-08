using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.ReconciliationReview;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfReconciliationCaseRepository : IReconciliationCaseRepository
{
    private readonly AppDbContext _db;

    public EfReconciliationCaseRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<ReconciliationRun?> GetByBatchRunIdAsync(Guid batchRunId, CancellationToken ct = default)
        => _db.ReconciliationRuns
            .Include(x => x.Cases)
            .FirstOrDefaultAsync(x => x.BatchRunId == batchRunId, ct);

    public async Task DeleteByBatchRunIdAsync(Guid batchRunId, CancellationToken ct = default)
    {
        var run = await _db.ReconciliationRuns
            .Include(x => x.Cases)
            .FirstOrDefaultAsync(x => x.BatchRunId == batchRunId, ct);

        if (run is null) return;

        _db.ReconciliationCases.RemoveRange(run.Cases);
        _db.ReconciliationRuns.Remove(run);
    }

    public Task AddRunAsync(ReconciliationRun run, CancellationToken ct = default)
    {
        _db.ReconciliationRuns.Add(run);
        return Task.CompletedTask;
    }

    public Task AddCasesAsync(IEnumerable<ReconciliationCase> cases, CancellationToken ct = default)
    {
        _db.ReconciliationCases.AddRange(cases);
        return Task.CompletedTask;
    }
}
