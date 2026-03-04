using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Domain.Entities.Reconciliation;

public sealed class ReconciliationMatch
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid BatchRunId { get; private set; }
    public BatchRun BatchRun { get; private set; } = default!;

    public int DebtRowNumber { get; private set; }
    public int PaymentRowNumber { get; private set; }

    public string CustomerId { get; private set; } = default!;
    public decimal Amount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ReconciliationMatch() { } // EF

    public ReconciliationMatch(Guid batchRunId, int debtRowNumber, int paymentRowNumber, string customerId, decimal amount)
    {
        if (debtRowNumber <= 0) throw new ArgumentOutOfRangeException(nameof(debtRowNumber));
        if (paymentRowNumber <= 0) throw new ArgumentOutOfRangeException(nameof(paymentRowNumber));
        if (string.IsNullOrWhiteSpace(customerId)) throw new ArgumentException("CustomerId required", nameof(customerId));

        BatchRunId = batchRunId;
        DebtRowNumber = debtRowNumber;
        PaymentRowNumber = paymentRowNumber;
        CustomerId = customerId;
        Amount = amount;
    }
}
