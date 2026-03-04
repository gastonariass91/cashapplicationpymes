using ReconciliationApp.Domain.Entities.Imports;

namespace ReconciliationApp.Application.Abstractions.Repositories;

public interface IImportRowRepository
{
    Task<List<ReconciliationApp.Domain.Entities.Imports.ImportRow>> ListByRunIdAsync(Guid batchRunId, CancellationToken ct);

    Task<Guid?> GetRunIdAsync(Guid batchId, int runNumber, CancellationToken ct);
    Task DeleteByRunAndTypeAsync(Guid batchRunId, ImportType type, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<ImportRow> rows, CancellationToken ct);
}
