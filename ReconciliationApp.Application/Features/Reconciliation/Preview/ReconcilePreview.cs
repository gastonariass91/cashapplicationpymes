using ReconciliationApp.Domain.Entities.Imports;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Imports;
using ReconciliationApp.Domain.Enums;

namespace ReconciliationApp.Application.Features.Reconciliation.Preview;

public static class ReconcilePreview
{
    public sealed record MatchRow(
        int debtRowNumber,
        int paymentRowNumber,
        string customerId,
        decimal amount
    );

    public sealed record PreviewResult(
        Guid batchRunId,
        IReadOnlyList<MatchRow> matches,
        IReadOnlyList<int> unmatchedDebtRowNumbers,
        IReadOnlyList<int> unmatchedPaymentRowNumbers
    );

    public static async Task<PreviewResult> ExecuteAsync(
        Guid batchId,
        int runNumber,
        IBatchRepository batches,
        IBatchRunRepository batchRuns,
        IImportRowRepository importRows,
        CancellationToken ct
    )
    {
        var batch = await batches.GetByIdAsync(batchId, ct);
        if (batch is null) throw new InvalidOperationException("Batch not found.");

        var run = await batchRuns.GetByBatchAndRunNumberAsync(batchId, runNumber, ct);
        if (run is null) throw new InvalidOperationException("Run not found.");

        var rows = await importRows.ListByRunIdAsync(run.Id, ct);

        var debts = rows.Where(r => r.Type == ImportType.Debt).ToList();
        var pays = rows.Where(r => r.Type == ImportType.Payments).ToList();

        var paymentBuckets = new Dictionary<(string, decimal), Queue<int>>();

        foreach (var p in pays)
        {
            var data = ImportRowParser.ParsePayment(p.DataJson);
            var key = (data.CustomerId, data.Amount);

            if (!paymentBuckets.TryGetValue(key, out var q))
            {
                q = new Queue<int>();
                paymentBuckets[key] = q;
            }

            q.Enqueue(p.RowNumber);
        }

        var matches = new List<MatchRow>();
        var matchedDebt = new HashSet<int>();
        var matchedPay = new HashSet<int>();

        foreach (var d in debts)
        {
            var data = ImportRowParser.ParseDebt(d.DataJson);
            var key = (data.CustomerId, data.Amount);

            if (paymentBuckets.TryGetValue(key, out var q) && q.Count > 0)
            {
                var paymentRowNumber = q.Dequeue();

                matches.Add(new MatchRow(
                    debtRowNumber: d.RowNumber,
                    paymentRowNumber: paymentRowNumber,
                    customerId: data.CustomerId,
                    amount: data.Amount
                ));

                matchedDebt.Add(d.RowNumber);
                matchedPay.Add(paymentRowNumber);
            }
        }

        var unmatchedDebt = debts
            .Where(d => !matchedDebt.Contains(d.RowNumber))
            .Select(d => d.RowNumber)
            .OrderBy(x => x)
            .ToList();

        var unmatchedPay = pays
            .Where(p => !matchedPay.Contains(p.RowNumber))
            .Select(p => p.RowNumber)
            .OrderBy(x => x)
            .ToList();

        return new PreviewResult(run.Id, matches, unmatchedDebt, unmatchedPay);
    }
}
