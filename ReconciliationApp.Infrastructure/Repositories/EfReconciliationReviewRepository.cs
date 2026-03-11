using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.ReconciliationReview;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.Infrastructure.Repositories;

public sealed class EfReconciliationReviewRepository : IReconciliationReviewRepository
{
    private readonly AppDbContext _db;

    public EfReconciliationReviewRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReconciliationRun?> GetRunAsync(string runId, CancellationToken ct = default)
    {
        var batchRun = await ResolveBatchRunAsync(runId, ct);
        if (batchRun is null) return null;

        return await _db.ReconciliationRuns
            .AsNoTracking()
            .Include(x => x.Cases)
            .FirstOrDefaultAsync(x => x.BatchRunId == batchRun.Id, ct);
    }

    public async Task<bool> SeedRunIfMissingAsync(string runId, CancellationToken ct = default)
    {
        var batchRun = await ResolveBatchRunAsync(runId, ct);
        if (batchRun is null)
            return false;

        var existing = await _db.ReconciliationRuns
            .FirstOrDefaultAsync(x => x.BatchRunId == batchRun.Id, ct);

        if (existing is not null)
            return true;

        var reviewRun = new ReconciliationRun(batchRun.Id, BuildLegacyPublicRunId(batchRun.RunNumber));

        var seededCases = new List<ReconciliationCase>
        {
            new(reviewRun.Id, "case-1-1", 1, 1, "C1", 1000m, 1000m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto · ref coincide", "Aceptar"),
            new(reviewRun.Id, "case-4-3", 4, 3, "C3", 1200m, 1200m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto", "Aceptar"),
            new(reviewRun.Id, "case-5-4", 5, 4, "C4", 700m, 700m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto", "Aceptar"),
            new(reviewRun.Id, "case-7-6", 7, 6, "C5", 350m, 350m, 0m, "Cliente+Monto", "ok", "medium", "exact", "Cliente coincide · sin ref", "Aceptar (c/ cuidado)"),
            new(reviewRun.Id, "case-2-2", 2, 2, "C1", 500m, 450m, -50m, "Monto cercano", "pending", "medium", "partial", "Pago menor · posible parcial", "Revisar parcial"),
            new(reviewRun.Id, "case-6-8", 6, 8, "C2", 900m, 930m, 30m, "Monto cercano", "pending", "medium", "ambiguous", "Varias deudas posibles", "Revisar"),
            new(reviewRun.Id, "case-3-5", 3, 5, "C2", 200m, 200m, 0m, "Duplicado?", "exception", "low", "duplicate", "Pago similar ya conciliado", "Excepción")
        };

        _db.ReconciliationRuns.Add(reviewRun);
        await _db.ReconciliationCases.AddRangeAsync(seededCases, ct);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> AcceptCaseAsync(string runId, string caseId, CancellationToken ct = default)
    {
        var run = await GetTrackedRunAsync(runId, ct);
        if (run is null) return false;

        var item = run.Cases.FirstOrDefault(x => x.CaseId == caseId);
        if (item is null) return false;

        item.Accept();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MarkExceptionAsync(string runId, string caseId, CancellationToken ct = default)
    {
        var run = await GetTrackedRunAsync(runId, ct);
        if (run is null) return false;

        var item = run.Cases.FirstOrDefault(x => x.CaseId == caseId);
        if (item is null) return false;

        item.MarkException();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> BulkAcceptAsync(string runId, IEnumerable<string> caseIds, CancellationToken ct = default)
    {
        var ids = caseIds.Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0) return 0;

        var run = await GetTrackedRunAsync(runId, ct);
        if (run is null) return 0;

        var items = run.Cases.Where(x => ids.Contains(x.CaseId)).ToList();
        foreach (var item in items)
            item.Accept();

        await _db.SaveChangesAsync(ct);
        return items.Count;
    }

    public async Task<(bool CanConfirm, string Status)> ConfirmAsync(string runId, CancellationToken ct = default)
    {
        var run = await GetTrackedRunAsync(runId, ct);
        if (run is null) return (false, "in_review");

        var hasPending = run.Cases.Any(c => c.Status == "pending");
        var hasException = run.Cases.Any(c => c.Status == "exception");

        if (hasPending || hasException)
            return (false, run.Status);

        run.Confirm();
        await _db.SaveChangesAsync(ct);
        return (true, run.Status);
    }

    private async Task<ReconciliationRun?> GetTrackedRunAsync(string runId, CancellationToken ct)
    {
        var batchRun = await ResolveBatchRunAsync(runId, ct);
        if (batchRun is null) return null;

        return await _db.ReconciliationRuns
            .Include(x => x.Cases)
            .FirstOrDefaultAsync(x => x.BatchRunId == batchRun.Id, ct);
    }

    private async Task<BatchRun?> ResolveBatchRunAsync(string runId, CancellationToken ct)
    {
        if (Guid.TryParse(runId, out var batchRunId))
        {
            return await _db.BatchRuns
                .FirstOrDefaultAsync(x => x.Id == batchRunId, ct);
        }

        var existingReviewRun = await _db.ReconciliationRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicRunId == runId, ct);

        if (existingReviewRun is not null)
        {
            return await _db.BatchRuns
                .FirstOrDefaultAsync(x => x.Id == existingReviewRun.BatchRunId, ct);
        }

        var runNumber = ExtractRunNumber(runId);
        if (runNumber is null) return null;

        return await _db.BatchRuns
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.RunNumber == runNumber.Value, ct);
    }

    private static int? ExtractRunNumber(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId)) return null;

        var parts = runId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;

        return int.TryParse(parts[^1], out var value) ? value : null;
    }

    private static string BuildLegacyPublicRunId(int runNumber)
        => $"run-2026-03-{runNumber:0000}";
}
