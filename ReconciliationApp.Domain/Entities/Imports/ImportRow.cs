using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Domain.Entities.Imports;

public sealed class ImportRow
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid BatchRunId { get; private set; }
    public BatchRun BatchRun { get; private set; } = default!;

    public ImportType Type { get; private set; }

    public int RowNumber { get; private set; }

    public string DataJson { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private ImportRow() { } // EF

    public ImportRow(Guid batchRunId, ImportType type, int rowNumber, string dataJson)
    {
        if (rowNumber <= 0) throw new ArgumentOutOfRangeException(nameof(rowNumber));
        if (string.IsNullOrWhiteSpace(dataJson)) throw new ArgumentException("DataJson required", nameof(dataJson));

        BatchRunId = batchRunId;
        Type = type;
        RowNumber = rowNumber;
        DataJson = dataJson;
    }
}
