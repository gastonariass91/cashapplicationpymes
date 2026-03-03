using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Sql;

public sealed class RunNumberService
{
    private readonly AppDbContext _db;

    public RunNumberService(AppDbContext db) => _db = db;

    public async Task<int> IncrementAndGetRunNumberAsync(Guid batchId, CancellationToken ct)
    {
        // Incrementa de forma atómica y devuelve el nuevo valor
        var sql = """
        UPDATE reconciliation_batches
        SET current_run_number = current_run_number + 1
        WHERE "Id" = {0}
        RETURNING current_run_number;
        """;

        // Npgsql: usamos FromSqlRaw sobre un tipo keyless simple
        var result = await _db.Set<RunNumberRow>()
            .FromSqlRaw(sql, batchId)
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

        if (result is null)
            throw new InvalidOperationException("Batch not found.");

        return result.current_run_number;
    }

    private sealed class RunNumberRow
    {
        public int current_run_number { get; set; }
    }
}
