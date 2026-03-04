using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileRun;

namespace ReconciliationApp.Application.Features.Reconciliation.ReconcileResult;

public sealed class ReconcileResultHandler
{
    private readonly IBatchRunRepository _batchRuns;
    private readonly IReconciliationMatchRepository _matches;

    public ReconcileResultHandler(IBatchRunRepository batchRuns, IReconciliationMatchRepository matches)
    {
        _batchRuns = batchRuns;
        _matches = matches;
    }

    public async Task<ReconcileRunResult?> Handle(Guid batchId, int runNumber, CancellationToken ct)
    {
        var run = await _batchRuns.GetByBatchAndRunNumberAsync(batchId, runNumber, ct);
        if (run is null) return null;

        var existing = await _matches.ListByRunIdAsync(run.Id, ct);

        return new ReconcileRunResult(
            batchRunId: run.Id,
            reconciledAt: run.ReconciledAt,
            matchesSaved: existing.Count,
            matches: existing.Select(x => new ReconcileMatchDto(x.DebtRowNumber, x.PaymentRowNumber, x.CustomerId, x.Amount)).ToList(),
            unmatchedDebtRowNumbers: Array.Empty<int>(),
            unmatchedPaymentRowNumbers: Array.Empty<int>(),
            alreadyReconciled: run.ReconciledAt is not null
        );
    }
}
