using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Imports;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.Imports;
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
            .Include(x => x.BatchRun)
                .ThenInclude(x => x.Batch)
            .FirstOrDefaultAsync(x => x.BatchRunId == batchRun.Id, ct);
    }

    public async Task<ReviewRunTotals?> GetRunTotalsAsync(string runId, CancellationToken ct = default)
    {
        var batchRun = await ResolveBatchRunAsync(runId, ct);
        if (batchRun is null) return null;

        var rows = await _db.ImportRows
            .AsNoTracking()
            .Where(x => x.BatchRunId == batchRun.Id)
            .ToListAsync(ct);

        var debtRows = rows.Where(x => x.Type == ImportType.Debt).ToList();
        var paymentRows = rows.Where(x => x.Type == ImportType.Payments).ToList();

        return new ReviewRunTotals(
            DebtsRowsTotal: debtRows.Count,
            PaymentsRowsTotal: paymentRows.Count,
            DebtsAmountTotal: debtRows.Sum(x => ImportRowParser.ExtractAmount(x.DataJson)),
            PaymentsAmountTotal: paymentRows.Sum(x => ImportRowParser.ExtractAmount(x.DataJson))
        );
    }

    public async Task<string?> GetRunCompanyNameAsync(string runId, CancellationToken ct = default)
    {
        var batchRun = await ResolveBatchRunAsync(runId, ct);
        if (batchRun is null) return null;

        var companyId = await _db.BatchRuns
            .AsNoTracking()
            .Where(x => x.Id == batchRun.Id)
            .Select(x => x.Batch.CompanyId)
            .FirstOrDefaultAsync(ct);

        if (companyId == Guid.Empty) return null;

        return await _db.Companies
            .AsNoTracking()
            .Where(x => x.Id == companyId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct);
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
            .Include(x => x.BatchRun)
                .ThenInclude(x => x.Batch)
            .FirstOrDefaultAsync(x => x.BatchRunId == batchRun.Id, ct);
    }

    private async Task<BatchRun?> ResolveBatchRunAsync(string runId, CancellationToken ct)
    {
        if (!Guid.TryParse(runId, out var batchRunId))
            return null;

        return await _db.BatchRuns
            .Include(x => x.Batch)
            .FirstOrDefaultAsync(x => x.Id == batchRunId, ct);
    }
}
