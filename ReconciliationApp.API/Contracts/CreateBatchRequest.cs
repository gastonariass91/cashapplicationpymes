namespace ReconciliationApp.API.Contracts;

public sealed record CreateBatchRequest(Guid CompanyId, DateOnly PeriodFrom, DateOnly PeriodTo);
