namespace ReconciliationApp.Domain.Entities.Batching;

public sealed class BatchRun
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid BatchId { get; private set; }

    public ReconciliationBatch Batch { get; private set; } = default!; // 🔥 navegación

    public int RunNumber { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReconciledAt { get; private set; }

    public void MarkReconciled(DateTimeOffset? at = null)
    {
        if (ReconciledAt is not null) return;
        ReconciledAt = at ?? DateTimeOffset.UtcNow;
    }

    private BatchRun() { } // EF

    public BatchRun(Guid batchId, int runNumber)
    {
        if (runNumber <= 0) throw new ArgumentOutOfRangeException(nameof(runNumber));

        BatchId = batchId;
        RunNumber = runNumber;
    }
}