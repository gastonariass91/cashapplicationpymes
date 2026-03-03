using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfCompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _db;

    public EfCompanyRepository(AppDbContext db) => _db = db;

    public void Add(Company company) => _db.Companies.Add(company);

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        _db.Companies.AnyAsync(x => x.Name == name, ct);
}
