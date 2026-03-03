namespace ReconciliationApp.Application.Features.Batches.CreateRun;

public sealed record CreateRunResult(Guid BatchId, int RunNumber, DateTimeOffset CreatedAt);
