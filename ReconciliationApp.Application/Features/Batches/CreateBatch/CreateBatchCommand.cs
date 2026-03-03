namespace ReconciliationApp.Application.Features.Batches.CreateBatch;

public sealed record CreateBatchCommand(Guid CompanyId, DateOnly PeriodFrom, DateOnly PeriodTo);
