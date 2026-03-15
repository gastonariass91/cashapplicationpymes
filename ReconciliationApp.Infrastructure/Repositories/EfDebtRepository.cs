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

    public Task<List<Debt>> ListByCompanyAsync(Guid companyId, CancellationToken ct)
    {
        return _db.Debts
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.IssueDate)
            .ThenBy(x => x.InvoiceNumber)
            .ToListAsync(ct);
    }

    public Task<List<Debt>> ListOpenByCompanyAsync(Guid companyId, CancellationToken ct)
    {
        return _db.Debts
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.CompanyId == companyId && x.OutstandingAmount > 0)
            .OrderByDescending(x => x.IssueDate)
            .ThenBy(x => x.InvoiceNumber)
            .ToListAsync(ct);
    }

    public Task<Debt?> GetByCompanyCustomerAndInvoiceAsync(
        Guid companyId,
        Guid customerId,
        string invoiceNumber,
        CancellationToken ct)
    {
        return _db.Debts.FirstOrDefaultAsync(
            x => x.CompanyId == companyId
                 && x.CustomerId == customerId
                 && x.InvoiceNumber == invoiceNumber,
            ct);
    }

    public Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct)
    {
        return _db.Debts
            .Where(x => x.SourceBatchRunId == sourceBatchRunId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddAsync(Debt debt, CancellationToken ct)
    {
        await _db.Debts.AddAsync(debt, ct);
    }

    public async Task AddRangeAsync(IEnumerable<Debt> debts, CancellationToken ct)
    {
        await _db.Debts.AddRangeAsync(debts, ct);
    }
}