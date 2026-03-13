using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> ListByCompanyAsync(Guid companyId, CancellationToken ct);
    Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Payment> payments, CancellationToken ct);
}