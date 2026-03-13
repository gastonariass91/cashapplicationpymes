using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfDebtRepository : IDebtRepository
{
    private readonly AppDbContext _db;

    public EfDebtRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct)
    {
        return _db.Debts
            .Where(x => x.SourceBatchRunId == sourceBatchRunId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Debt> debts, CancellationToken ct)
    {
        await _db.Debts.AddRangeAsync(debts, ct);
    }
}
