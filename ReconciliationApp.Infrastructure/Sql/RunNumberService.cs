using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Sql;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Sql;

public sealed class RunNumberService : IRunNumberService
{
    private readonly AppDbContext _db;
    public RunNumberService(AppDbContext db) => _db = db;

    public async Task<int> IncrementAndGetAsync(Guid batchId, CancellationToken ct)
    {
        // 1) Incremento atómico en DB (sin cargar entidad)
        var affected = await _db.Batches
            .Where(b => b.Id == batchId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(b => b.CurrentRunNumber, b => b.CurrentRunNumber + 1),
                ct);

        if (affected != 1)
            throw new InvalidOperationException("Batch not found.");

        // 2) Leer el valor actualizado
        var current = await _db.Batches
            .Where(b => b.Id == batchId)
            .Select(b => b.CurrentRunNumber)
            .SingleAsync(ct);

        return current;
    }
}
