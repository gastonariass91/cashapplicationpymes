namespace ReconciliationApp.Domain.Entities.Batching;

public sealed class BatchRun
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid BatchId { get; private set; }
    public int RunNumber { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private BatchRun() { } // EF

    public BatchRun(Guid batchId, int runNumber)
    {
        if (runNumber <= 0) throw new ArgumentOutOfRangeException(nameof(runNumber));
        BatchId = batchId;
        RunNumber = runNumber;
    }
}
