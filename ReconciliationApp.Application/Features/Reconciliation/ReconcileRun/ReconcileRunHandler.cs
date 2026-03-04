using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Reconciliation.Preview;
using ReconciliationApp.Domain.Entities.Reconciliation;

namespace ReconciliationApp.Application.Features.Reconciliation.ReconcileRun;

public sealed class ReconcileRunHandler
{
    private readonly IBatchRepository _batches;
    private readonly IBatchRunRepository _batchRuns;
    private readonly IImportRowRepository _importRows;
    private readonly IReconciliationMatchRepository _matches;
    private readonly IUnitOfWork _uow;

    public ReconcileRunHandler(
        IBatchRepository batches,
        IBatchRunRepository batchRuns,
        IImportRowRepository importRows,
        IReconciliationMatchRepository matches,
        IUnitOfWork uow)
    {
        _batches = batches;
        _batchRuns = batchRuns;
        _importRows = importRows;
        _matches = matches;
        _uow = uow;
    }

    public async Task<ReconcileRunResult?> Handle(Guid batchId, int runNumber, CancellationToken ct)
    {
        var run = await _batchRuns.GetByBatchAndRunNumberAsync(batchId, runNumber, ct);
        if (run is null) return null;

        // Si ya está reconciliado, devolvemos lo persistido
        if (run.ReconciledAt is not null)
        {
            var existing = await _matches.ListByRunIdAsync(run.Id, ct);

            return new ReconcileRunResult(
                batchRunId: run.Id,
                reconciledAt: run.ReconciledAt,
                matchesSaved: existing.Count,
                matches: existing.Select(x => new ReconcileMatchDto(x.DebtRowNumber, x.PaymentRowNumber, x.CustomerId, x.Amount)).ToList(),
                unmatchedDebtRowNumbers: Array.Empty<int>(),
                unmatchedPaymentRowNumbers: Array.Empty<int>(),
                alreadyReconciled: true
            );
        }

        // Calcula preview (MVP)
        var preview = await ReconcilePreview.ExecuteAsync(batchId, runNumber, _batches, _batchRuns, _importRows, ct);

        // Borra y persiste matches
        await _matches.DeleteByRunIdAsync(run.Id, ct);

        var entities = preview.matches
            .Select(m => new ReconciliationMatch(run.Id, m.debtRowNumber, m.paymentRowNumber, m.customerId, m.amount))
            .ToList();

        await _matches.AddRangeAsync(entities, ct);

        run.MarkReconciled();
        await _uow.SaveChangesAsync(ct);

        return new ReconcileRunResult(
            batchRunId: run.Id,
            reconciledAt: run.ReconciledAt,
            matchesSaved: entities.Count,
            matches: entities.Select(x => new ReconcileMatchDto(x.DebtRowNumber, x.PaymentRowNumber, x.CustomerId, x.Amount)).ToList(),
            unmatchedDebtRowNumbers: preview.unmatchedDebtRowNumbers.ToList(),
            unmatchedPaymentRowNumbers: preview.unmatchedPaymentRowNumbers.ToList(),
            alreadyReconciled: false
        );
    }
}
