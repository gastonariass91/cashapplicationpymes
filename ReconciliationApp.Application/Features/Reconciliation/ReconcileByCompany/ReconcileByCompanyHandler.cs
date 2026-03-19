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

    public ReconcileByCompanyHandler(
        IBatchRepository batches,
        IBatchRunRepository batchRuns,
        ReconcileRun.ReconcileRunHandler reconcileRun)
    {
        _batches = batches;
        _batchRuns = batchRuns;
        _reconcileRun = reconcileRun;
    }

    /// <summary>
    /// Verifica que el batch pertenezca a la compañía, resetea el estado
    /// del run para permitir re-conciliar, y dispara el motor.
    /// Si algo falla silenciosamente no rompe el import — el dato ya fue guardado.
    /// </summary>
    public async Task HandleAsync(Guid companyId, Guid batchId, int runNumber, CancellationToken ct)
    {
        var batch = await _batches.GetByIdAsync(batchId, ct);
        if (batch is null || batch.CompanyId != companyId)
            return;

        var run = await _batchRuns.GetByBatchAndRunNumberAsync(batchId, runNumber, ct);
        if (run is null)
            return;

        // Resetea ReconciledAt para que ReconcileRunHandler no devuelva el resultado cacheado
        run.ResetReconciled();

        await _reconcileRun.Handle(batchId, runNumber, ct);
    }
}
