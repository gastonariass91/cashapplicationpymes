using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IDebtRepository
{
    Task<List<Debt>> ListByCompanyAsync(Guid companyId, CancellationToken ct);
    Task<List<Debt>> ListOpenByCompanyAsync(Guid companyId, CancellationToken ct);
    Task<Debt?> GetByCompanyCustomerAndInvoiceAsync(Guid companyId, Guid customerId, string invoiceNumber, CancellationToken ct);

    Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct);

    Task AddAsync(Debt debt, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Debt> debts, CancellationToken ct);
}