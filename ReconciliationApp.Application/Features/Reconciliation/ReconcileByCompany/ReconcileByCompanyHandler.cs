using Microsoft.Extensions.Logging;
using ReconciliationApp.Application.Abstractions.Repositories;

namespace ReconciliationApp.Application.Features.Reconciliation.ReconcileByCompany;

/// <summary>
/// Dispara el motor de conciliación para el batch/run activo de una compañía.
/// Lo llaman los 3 handlers de import automáticamente después de cada CSV.
/// No rompe el flujo manual existente (POST /batches/{id}/runs/{n}/reconcile).
/// </summary>
public sealed class ReconcileByCompanyHandler
{
    private readonly IBatchRepository _batches;
    private readonly IBatchRunRepository _batchRuns;
    private readonly ReconcileRun.ReconcileRunHandler _reconcileRun;
    private readonly ILogger<ReconcileByCompanyHandler> _logger;

    public ReconcileByCompanyHandler(
        IBatchRepository batches,
        IBatchRunRepository batchRuns,
        ReconcileRun.ReconcileRunHandler reconcileRun,
        ILogger<ReconcileByCompanyHandler> logger)
    {
        _batches = batches;
        _batchRuns = batchRuns;
        _reconcileRun = reconcileRun;
        _logger = logger;
    }

    public async Task HandleAsync(Guid companyId, Guid batchId, int runNumber, CancellationToken ct)
    {
        _logger.LogInformation(
            "Auto-reconcile triggered. CompanyId={CompanyId} BatchId={BatchId} RunNumber={RunNumber}",
            companyId, batchId, runNumber);

        try
        {
            var batch = await _batches.GetByIdAsync(batchId, ct);
            if (batch is null || batch.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Auto-reconcile aborted: batch not found or company mismatch. BatchId={BatchId} CompanyId={CompanyId}",
                    batchId, companyId);
                return;
            }

            var run = await _batchRuns.GetByBatchAndRunNumberAsync(batchId, runNumber, ct);
            if (run is null)
            {
                _logger.LogWarning(
                    "Auto-reconcile aborted: run not found. BatchId={BatchId} RunNumber={RunNumber}",
                    batchId, runNumber);
                return;
            }

            run.ResetReconciled();

            var result = await _reconcileRun.Handle(batchId, runNumber, ct);

            if (result is null)
            {
                _logger.LogWarning(
                    "Auto-reconcile returned null result. BatchId={BatchId} RunNumber={RunNumber}",
                    batchId, runNumber);
                return;
            }

            _logger.LogInformation(
                "Auto-reconcile completed. BatchRunId={BatchRunId} Matches={Matches} UnmatchedDebts={UnmatchedDebts} UnmatchedPayments={UnmatchedPayments}",
                result.batchRunId, result.matchesSaved,
                result.unmatchedDebtRowNumbers.Count,
                result.unmatchedPaymentRowNumbers.Count);
        }
        catch (Exception ex)
        {
            // No re-lanzamos: el import ya se guardó correctamente.
            // El error queda logueado para diagnóstico pero no rompe el flujo del usuario.
            _logger.LogError(ex,
                "Auto-reconcile failed after import. CompanyId={CompanyId} BatchId={BatchId} RunNumber={RunNumber}",
                companyId, batchId, runNumber);
        }
    }
}
