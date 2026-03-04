using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Reconciliation;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfReconciliationMatchRepository : IReconciliationMatchRepository
{
    private readonly AppDbContext _db;

    public EfReconciliationMatchRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<ReconciliationMatch>> ListByRunIdAsync(Guid batchRunId, CancellationToken ct = default)
        => _db.ReconciliationMatches
            .AsNoTracking()
            .Where(x => x.BatchRunId == batchRunId)
            .OrderBy(x => x.DebtRowNumber)
            .ToListAsync(ct);

    public Task DeleteByRunIdAsync(Guid batchRunId, CancellationToken ct = default)
        => _db.ReconciliationMatches
            .Where(x => x.BatchRunId == batchRunId)
            .ExecuteDeleteAsync(ct);

    public Task AddRangeAsync(IEnumerable<ReconciliationMatch> matches, CancellationToken ct = default)
        => _db.ReconciliationMatches.AddRangeAsync(matches, ct);
}
