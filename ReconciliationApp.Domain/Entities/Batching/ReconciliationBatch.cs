using ReconciliationApp.Domain.Enums;

namespace ReconciliationApp.Domain.Entities.Batching;

public sealed class ReconciliationBatch
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CompanyId { get; private set; }

    public DateOnly PeriodFrom { get; private set; }
    public DateOnly PeriodTo { get; private set; }

    public BatchStatus Status { get; private set; } = BatchStatus.Draft;

    public int CurrentRunNumber { get; private set; } = 0;

    private readonly List<BatchRun> _runs = new();
    public IReadOnlyCollection<BatchRun> Runs => _runs.AsReadOnly();

    private ReconciliationBatch() { } // EF

    public ReconciliationBatch(Guid companyId, DateOnly periodFrom, DateOnly periodTo)
    {
        if (periodTo < periodFrom) throw new ArgumentException("PeriodTo must be >= PeriodFrom");

        CompanyId = companyId;
        PeriodFrom = periodFrom;
        PeriodTo = periodTo;
    }

    public BatchRun CreateNewRun()
    {
        CurrentRunNumber++;
        var run = new BatchRun(Id, CurrentRunNumber);
        _runs.Add(run);
        return run;
    }

    public void SetStatus(BatchStatus status) => Status = status;
}
