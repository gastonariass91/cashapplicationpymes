using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByCompanyAndCustomerKeyAsync(Guid companyId, string customerKey, CancellationToken ct);
    Task AddAsync(Customer customer, CancellationToken ct);
}
