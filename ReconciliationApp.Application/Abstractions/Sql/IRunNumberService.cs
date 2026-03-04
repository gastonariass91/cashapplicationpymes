namespace ReconciliationApp.Application.Abstractions.Sql;

public interface IRunNumberService
{
    Task<int> IncrementAndGetAsync(Guid batchId, CancellationToken ct);
}
