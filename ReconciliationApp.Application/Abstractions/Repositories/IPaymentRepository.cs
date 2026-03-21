using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> ListByCompanyAsync(Guid companyId, CancellationToken ct);
    Task<List<Payment>> ListPendingByCompanyAsync(Guid companyId, CancellationToken ct);
    Task<Payment?> GetByCompanyAndPaymentNumberAsync(Guid companyId, string paymentNumber, CancellationToken ct);

    Task DeleteBySourceBatchRunIdAsync(Guid sourceBatchRunId, CancellationToken ct);

    Task AddAsync(Payment payment, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Payment> payments, CancellationToken ct);
}