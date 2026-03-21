using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfCustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public EfCustomerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Customer?> GetByCompanyAndCustomerKeyAsync(Guid companyId, string customerKey, CancellationToken ct)
    {
        return _db.Customers
            .FirstOrDefaultAsync(
                x => x.CompanyId == companyId && x.CustomerKey == customerKey,
                ct);
    }

    public Task<List<Customer>> ListByCompanyAsync(Guid companyId, CancellationToken ct)
    {
        return _db.Customers
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Customer customer, CancellationToken ct)
    {
        await _db.Customers.AddAsync(customer, ct);
    }
}