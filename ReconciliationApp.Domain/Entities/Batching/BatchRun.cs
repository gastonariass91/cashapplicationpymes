namespace ReconciliationApp.Domain.Entities.Batching;

public sealed class BatchRun
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid BatchId { get; private set; }

    public ReconciliationBatch Batch { get; private set; } = default!;

    public int RunNumber { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReconciledAt { get; private set; }

    public void MarkReconciled(DateTimeOffset? at = null)
    {
        ReconciledAt = at ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Permite re-ejecutar el motor de conciliación sobre este run.
    /// Usado por el modelo online: cada import actualiza el estado actual.
    /// </summary>
    public void ResetReconciled()
    {
        ReconciledAt = null;
    }

    private BatchRun() { } // EF

    public BatchRun(Guid batchId, int runNumber)
    {
        if (runNumber <= 0) throw new ArgumentOutOfRangeException(nameof(runNumber));

        BatchId = batchId;
        RunNumber = runNumber;
    }
}
