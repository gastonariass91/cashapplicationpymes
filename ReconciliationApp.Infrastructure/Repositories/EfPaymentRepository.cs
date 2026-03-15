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

    public Task<List<Payment>> ListByCompanyAsync(Guid companyId, CancellationToken ct)
    {
        return _db.Payments
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.PaymentDate)
            .ThenBy(x => x.PaymentNumber)
            .ToListAsync(ct);
    }

    public Task<List<Payment>> ListPendingByCompanyAsync(Guid companyId, CancellationToken ct)
    {
        return _db.Payments
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x =>
                x.CompanyId == companyId &&
                (x.Status == "Available" || x.Status == "Unidentified" || x.Status == "PartiallyApplied"))
            .OrderByDescending(x => x.PaymentDate)
            .ThenBy(x => x.PaymentNumber)
            .ToListAsync(ct);
    }

    public Task<Payment?> GetByCompanyAndPaymentNumberAsync(
        Guid companyId,
        string paymentNumber,
        CancellationToken ct)
    {
        return _db.Payments.FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.PaymentNumber == paymentNumber,
            ct);
    }

    public Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct)
    {
        return _db.Payments
            .Where(x => x.SourceBatchRunId == sourceBatchRunId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddAsync(Payment payment, CancellationToken ct)
    {
        await _db.Payments.AddAsync(payment, ct);
    }

    public async Task AddRangeAsync(IEnumerable<Payment> payments, CancellationToken ct)
    {
        await _db.Payments.AddRangeAsync(payments, ct);
    }
}