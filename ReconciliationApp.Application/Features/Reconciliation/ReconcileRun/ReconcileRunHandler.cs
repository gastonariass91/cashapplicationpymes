using System.Text.Json;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Reconciliation.Preview;
using ReconciliationApp.Domain.Entities.Imports;
using ReconciliationApp.Domain.Entities.Reconciliation;
using ReconciliationApp.Domain.Entities.ReconciliationReview;
using ReconciliationApp.Domain.Enums;

namespace ReconciliationApp.Application.Features.Reconciliation.ReconcileRun;

public sealed class ReconcileRunHandler
{
    private readonly IBatchRepository _batches;
    private readonly IBatchRunRepository _batchRuns;
    private readonly IImportRowRepository _importRows;
    private readonly IReconciliationMatchRepository _matches;
    private readonly IReconciliationCaseRepository _cases;
    private readonly IUnitOfWork _uow;

    public ReconcileRunHandler(
        IBatchRepository batches,
        IBatchRunRepository batchRuns,
        IImportRowRepository importRows,
        IReconciliationMatchRepository matches,
        IReconciliationCaseRepository cases,
        IUnitOfWork uow)
    {
        _batches = batches;
        _batchRuns = batchRuns;
        _importRows = importRows;
        _matches = matches;
        _cases = cases;
        _uow = uow;
    }

    public async Task<ReconcileRunResult?> Handle(Guid batchId, int runNumber, CancellationToken ct)
    {
        var run = await _batchRuns.GetByBatchAndRunNumberAsync(batchId, runNumber, ct);
        if (run is null) return null;

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

        var preview = await ReconcilePreview.ExecuteAsync(batchId, runNumber, _batches, _batchRuns, _importRows, ct);

        await _matches.DeleteByRunIdAsync(run.Id, ct);

        var entities = preview.matches
            .Select(m => new ReconciliationMatch(run.Id, m.debtRowNumber, m.paymentRowNumber, m.customerId, m.amount))
            .ToList();

        await _matches.AddRangeAsync(entities, ct);

        var importRows = await _importRows.ListByRunIdAsync(run.Id, ct);
        var reviewRun = new ReconciliationRun(run.Id, BuildPublicRunId(runNumber));
        var reviewCases = BuildReviewCases(reviewRun.Id, importRows, preview);

        await _cases.DeleteByBatchRunIdAsync(run.Id, ct);
        await _cases.AddRunAsync(reviewRun, ct);
        await _cases.AddCasesAsync(reviewCases, ct);

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

    private static List<ReconciliationCase> BuildReviewCases(
        Guid reviewRunId,
        List<ImportRow> rows,
        ReconcilePreview.PreviewResult preview)
    {
        var allRows = rows.ToDictionary(r => (r.Type, r.RowNumber));

        var result = new List<ReconciliationCase>();

        foreach (var match in preview.matches)
        {
            var debt = allRows[(ImportType.Debt, match.debtRowNumber)];
            var pay = allRows[(ImportType.Payments, match.paymentRowNumber)];

            var debtData = Parse(debt.DataJson);
            var payData = Parse(pay.DataJson);

            result.Add(new ReconciliationCase(
                reviewRunId,
                $"case-{match.debtRowNumber}-{match.paymentRowNumber}",
                match.debtRowNumber,
                match.paymentRowNumber,
                debtData.customerId,
                debtData.amount,
                payData.amount,
                payData.amount - debtData.amount,
                "Cliente+Monto",
                "ok",
                "high",
                "exact",
                "Mismo cliente · monto exacto",
                "Aceptar"
            ));
        }

        foreach (var debtRow in preview.unmatchedDebtRowNumbers)
        {
            var debt = allRows[(ImportType.Debt, debtRow)];
            var debtData = Parse(debt.DataJson);

            result.Add(new ReconciliationCase(
                reviewRunId,
                $"debt-only-{debtRow}",
                debtRow,
                null,
                debtData.customerId,
                debtData.amount,
                0m,
                -debtData.amount,
                "Sin match",
                "pending",
                "medium",
                "no_match",
                "No se encontró pago compatible",
                "Revisar"
            ));
        }

        foreach (var payRow in preview.unmatchedPaymentRowNumbers)
        {
            var pay = allRows[(ImportType.Payments, payRow)];
            var payData = Parse(pay.DataJson);

            result.Add(new ReconciliationCase(
                reviewRunId,
                $"pay-only-{payRow}",
                null,
                payRow,
                payData.customerId,
                0m,
                payData.amount,
                payData.amount,
                "Sin match",
                "pending",
                "medium",
                "no_match",
                "No se encontró deuda compatible",
                "Revisar"
            ));
        }

        return result
            .OrderBy(x => x.DebtRowNumber ?? int.MaxValue)
            .ThenBy(x => x.PaymentRowNumber ?? int.MaxValue)
            .ToList();
    }

    private static string BuildPublicRunId(int runNumber)
        => $"run-2026-03-{runNumber:0000}";

    private static (string customerId, decimal amount) Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var customerId = root.GetProperty("customer_id").GetString() ?? "";
        var amountStr = root.GetProperty("amount").GetString() ?? "0";

        decimal amount = 0m;
        if (!decimal.TryParse(amountStr, out amount))
        {
            if (root.GetProperty("amount").ValueKind == JsonValueKind.Number)
                amount = root.GetProperty("amount").GetDecimal();
        }

        return (customerId, amount);
    }
}
