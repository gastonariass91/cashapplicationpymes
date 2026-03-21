using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfBatchRepository : IBatchRepository
{
    private readonly AppDbContext _db;

    public EfBatchRepository(AppDbContext db) => _db = db;

    public void Add(ReconciliationBatch batch) => _db.Batches.Add(batch);

    public Task<ReconciliationBatch?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Batches
            .Include(x => x.Runs)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<ReconciliationBatch?> GetByCompanyAndPeriodAsync(
        Guid companyId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default) =>
        _db.Batches
            .Include(x => x.Runs)
            .FirstOrDefaultAsync(
                x => x.CompanyId == companyId && x.PeriodFrom == from && x.PeriodTo == to,
                ct);

    public Task<bool> ExistsForPeriodAsync(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        _db.Batches.AnyAsync(x => x.CompanyId == companyId && x.PeriodFrom == from && x.PeriodTo == to, ct);
}