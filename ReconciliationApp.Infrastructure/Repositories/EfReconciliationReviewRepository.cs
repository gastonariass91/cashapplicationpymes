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
            .Include(x => x.BatchRun)
                .ThenInclude(x => x.Batch)
            .FirstOrDefaultAsync(x => x.BatchRunId == batchRun.Id, ct);
    }

    public async Task<ReviewRunTotals?> GetRunTotalsAsync(string runId, CancellationToken ct = default)
    {
        var batchRun = await ResolveBatchRunAsync(runId, ct);
        if (batchRun is null) return null;

        return await BuildLiveTotalsAsync(batchRun.Batch.CompanyId, ct);
    }

    public async Task<string?> GetRunCompanyNameAsync(string runId, CancellationToken ct = default)
    {
        var batchRun = await ResolveBatchRunAsync(runId, ct);
        if (batchRun is null) return null;

        return await GetCompanyNameAsync(batchRun.Batch.CompanyId, ct);
    }

    public async Task<ReconciliationRun?> GetCurrentRunAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.ReconciliationRuns
            .AsNoTracking()
            .Include(x => x.Cases)
            .Include(x => x.BatchRun)
                .ThenInclude(x => x.Batch)
            .Where(x => x.BatchRun.Batch.CompanyId == companyId)
            .OrderByDescending(x => x.BatchRun.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ReviewRunTotals?> GetCurrentRunTotalsAsync(Guid companyId, CancellationToken ct = default)
    {
        return await BuildLiveTotalsAsync(companyId, ct);
    }

    public async Task<string?> GetCompanyNameAsync(Guid companyId, CancellationToken ct = default)
    {
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

    private async Task<ReviewRunTotals> BuildLiveTotalsAsync(Guid companyId, CancellationToken ct)
    {
        var debts = await _db.Debts
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.OutstandingAmount > 0)
            .ToListAsync(ct);

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId &&
                        (x.Status == "Available" || x.Status == "Unidentified" || x.Status == "PartiallyApplied"))
            .ToListAsync(ct);

        return new ReviewRunTotals(
            DebtsRowsTotal: debts.Count,
            PaymentsRowsTotal: payments.Count,
            DebtsAmountTotal: debts.Sum(x => x.OutstandingAmount),
            PaymentsAmountTotal: payments.Sum(x => x.Amount)
        );
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