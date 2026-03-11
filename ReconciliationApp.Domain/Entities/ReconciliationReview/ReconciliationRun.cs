using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Domain.Entities.ReconciliationReview;

public sealed class ReconciliationRun
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid BatchRunId { get; private set; }

    public BatchRun BatchRun { get; private set; } = default!;

    public string PublicRunId { get; private set; } = default!;

    public string Status { get; private set; } = "in_review";

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public List<ReconciliationCase> Cases { get; private set; } = new();

    private ReconciliationRun() { }

    public ReconciliationRun(Guid batchRunId, string publicRunId)
    {
        if (string.IsNullOrWhiteSpace(publicRunId))
            throw new ArgumentException("PublicRunId required", nameof(publicRunId));

        BatchRunId = batchRunId;
        PublicRunId = publicRunId;
    }

    public void Confirm()
    {
        if (ConfirmedAt is not null) return;

        ConfirmedAt = DateTimeOffset.UtcNow;
        Status = "confirmed";
    }
}
