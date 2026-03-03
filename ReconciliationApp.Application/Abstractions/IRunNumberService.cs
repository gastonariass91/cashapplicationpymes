namespace ReconciliationApp.Application.Abstractions;

public interface IRunNumberService
{
    Task<int> IncrementAndGetRunNumberAsync(Guid batchId, CancellationToken ct);
}
