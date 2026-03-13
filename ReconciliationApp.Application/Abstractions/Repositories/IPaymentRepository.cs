using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IPaymentRepository
{
    Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Payment> payments, CancellationToken ct);
}
