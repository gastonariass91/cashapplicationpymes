using ReconciliationApp.Application.Abstractions.Repositories;

namespace ReconciliationApp.Application.Features.Reconciliation.Preview;

public static class ReconcilePreview
{
    public sealed record DebtPreviewRow(
        int RowNumber,
        Guid DebtId,
        Guid CustomerId,
        string CustomerKey,
        string CustomerName,
        string InvoiceNumber,
        decimal Amount,
        decimal OutstandingAmount,
        string Currency
    );

    public sealed record PaymentPreviewRow(
        int RowNumber,
        Guid PaymentId,
        Guid? CustomerId,
        string CustomerKey,
        decimal Amount,
        string Currency,
        string PaymentNumber
    );

    public sealed record MatchRow(
        int debtRowNumber,
        int paymentRowNumber,
        Guid debtId,
        Guid paymentId,
        string customerId,
        decimal amount
    );

    public sealed record PreviewResult(
        Guid batchRunId,
        IReadOnlyList<DebtPreviewRow> debts,
        IReadOnlyList<PaymentPreviewRow> payments,
        IReadOnlyList<MatchRow> matches,
        IReadOnlyList<int> unmatchedDebtRowNumbers,
        IReadOnlyList<int> unmatchedPaymentRowNumbers
    );

    public static async Task<PreviewResult> ExecuteAsync(
        Guid batchId,
        int runNumber,
        IBatchRepository batches,
        IBatchRunRepository batchRuns,
        IDebtRepository debts,
        IPaymentRepository payments,
        CancellationToken ct
    )
    {
        var batch = await batches.GetByIdAsync(batchId, ct);
        if (batch is null) throw new InvalidOperationException("Batch not found.");

        var run = await batchRuns.GetByBatchAndRunNumberAsync(batchId, runNumber, ct);
        if (run is null) throw new InvalidOperationException("Run not found.");

        var liveDebts = await debts.ListOpenByCompanyAsync(batch.CompanyId, ct);
        var livePayments = await payments.ListPendingByCompanyAsync(batch.CompanyId, ct);

        var debtRows = liveDebts
            .OrderBy(x => x.CustomerId)
            .ThenBy(x => x.InvoiceNumber)
            .Select((x, idx) => new DebtPreviewRow(
                RowNumber: idx + 1,
                DebtId: x.Id,
                CustomerId: x.CustomerId,
                CustomerKey: x.Customer.CustomerKey,
                CustomerName: x.Customer.Name,
                InvoiceNumber: x.InvoiceNumber,
                Amount: x.Amount,
                OutstandingAmount: x.OutstandingAmount,
                Currency: x.Currency
            ))
            .ToList();

        var paymentRows = livePayments
            .OrderBy(x => x.CustomerId ?? Guid.Empty)
            .ThenBy(x => x.PaymentNumber)
            .Select((x, idx) => new PaymentPreviewRow(
                RowNumber: idx + 1,
                PaymentId: x.Id,
                CustomerId: x.CustomerId,
                CustomerKey: x.Customer?.CustomerKey ?? x.PayerTaxId ?? "",
                Amount: x.Amount,
                Currency: x.Currency,
                PaymentNumber: x.PaymentNumber
            ))
            .ToList();

        var paymentBuckets = new Dictionary<(Guid CustomerId, decimal Amount), Queue<PaymentPreviewRow>>();

        foreach (var p in paymentRows.Where(x => x.CustomerId.HasValue))
        {
            var key = (p.CustomerId!.Value, p.Amount);

            if (!paymentBuckets.TryGetValue(key, out var q))
            {
                q = new Queue<PaymentPreviewRow>();
                paymentBuckets[key] = q;
            }

            q.Enqueue(p);
        }

        var matches = new List<MatchRow>();
        var matchedDebt = new HashSet<int>();
        var matchedPay = new HashSet<int>();

        foreach (var d in debtRows)
        {
            var key = (d.CustomerId, d.OutstandingAmount);

            if (paymentBuckets.TryGetValue(key, out var q) && q.Count > 0)
            {
                var payment = q.Dequeue();

                matches.Add(new MatchRow(
                    debtRowNumber: d.RowNumber,
                    paymentRowNumber: payment.RowNumber,
                    debtId: d.DebtId,
                    paymentId: payment.PaymentId,
                    customerId: d.CustomerKey,
                    amount: d.OutstandingAmount
                ));

                matchedDebt.Add(d.RowNumber);
                matchedPay.Add(payment.RowNumber);
            }
        }

        var unmatchedDebt = debtRows
            .Where(d => !matchedDebt.Contains(d.RowNumber))
            .Select(d => d.RowNumber)
            .OrderBy(x => x)
            .ToList();

        var unmatchedPay = paymentRows
            .Where(p => !matchedPay.Contains(p.RowNumber))
            .Select(p => p.RowNumber)
            .OrderBy(x => x)
            .ToList();

        return new PreviewResult(
            batchRunId: run.Id,
            debts: debtRows,
            payments: paymentRows,
            matches: matches,
            unmatchedDebtRowNumbers: unmatchedDebt,
            unmatchedPaymentRowNumbers: unmatchedPay
        );
    }
}