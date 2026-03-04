using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Imports;
using ReconciliationApp.Domain.Enums;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfImportRowRepository : IImportRowRepository
{
    private readonly AppDbContext _db;

    public EfImportRowRepository(AppDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<ImportRow> rows, CancellationToken ct)
    {
        await _db.ImportRows.AddRangeAsync(rows, ct);
    }

    public async Task DeleteByRunAndTypeAsync(Guid batchRunId, ImportType type, CancellationToken ct)
    {
        await _db.ImportRows
            .Where(r => r.BatchRunId == batchRunId && r.Type == type)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<Guid?> GetRunIdAsync(Guid batchId, int runNumber, CancellationToken ct)
    {
        return await _db.BatchRuns
            .AsNoTracking()
            .Where(r => r.BatchId == batchId && r.RunNumber == runNumber)
            .Select(r => (Guid?)r.Id)
            .SingleOrDefaultAsync(ct);
    }

    public Task<List<ImportRow>> ListByRunIdAsync(Guid batchRunId, CancellationToken ct)
    {
        return _db.ImportRows
            .AsNoTracking()
            .Where(r => r.BatchRunId == batchRunId)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(ct);
    }
}
