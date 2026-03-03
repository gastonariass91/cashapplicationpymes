namespace ReconciliationApp.Application.Features.Batches.CreateBatch;

public sealed record CreateBatchResult(Guid BatchId, Guid CompanyId, DateOnly PeriodFrom, DateOnly PeriodTo);
