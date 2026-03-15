namespace ReconciliationApp.Application.Features.Imports;

public sealed record ImportErrorDto(
    int RowNumber,
    string Message
);

public sealed record ImportResult(
    string ImportType,
    int ProcessedCount,
    int InsertedCount,
    int UpdatedCount,
    int IgnoredCount,
    int ClosedCount,
    int ErrorCount,
    IReadOnlyList<ImportErrorDto> Errors
);