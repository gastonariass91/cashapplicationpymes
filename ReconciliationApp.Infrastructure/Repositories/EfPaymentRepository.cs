using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfPaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;

    public EfPaymentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct)
    {
        return _db.Payments
            .Where(x => x.SourceBatchRunId == sourceBatchRunId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Payment> payments, CancellationToken ct)
    {
        await _db.Payments.AddRangeAsync(payments, ct);
    }
}