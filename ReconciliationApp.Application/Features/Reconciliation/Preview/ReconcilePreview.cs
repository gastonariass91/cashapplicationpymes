using System.Text.Json;
using ReconciliationApp.Domain.Entities.Imports;
using ReconciliationApp.Application.Abstractions.Repositories;
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
        var pays  = rows.Where(r => r.Type == ImportType.Payments).ToList();

        static (string customerId, decimal amount) ParseDebt(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var customerId = GetRequiredString(root, "customer_id");
            var amount = GetRequiredDecimal(root, "amount");

            return (customerId, amount);
        }

        static (string customerId, decimal amount) ParsePayment(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var customerId = GetOptionalString(root, "payer_tax_id")
                          ?? GetOptionalString(root, "customer_id")
                          ?? "";

            var amount = GetRequiredDecimal(root, "amount");

            return (customerId, amount);
        }

        var paymentBuckets = new Dictionary<(string, decimal), Queue<(int rowNumber, string json)>>();

        foreach (var p in pays)
        {
            var (cid, amt) = ParsePayment(p.DataJson);
            var key = (cid, amt);

            if (!paymentBuckets.TryGetValue(key, out var q))
            {
                q = new Queue<(int, string)>();
                paymentBuckets[key] = q;
            }

            q.Enqueue((p.RowNumber, p.DataJson));
        }

        var matches = new List<MatchRow>();
        var matchedDebt = new HashSet<int>();
        var matchedPay  = new HashSet<int>();

        foreach (var d in debts)
        {
            var (cid, amt) = ParseDebt(d.DataJson);
            var key = (cid, amt);

            if (paymentBuckets.TryGetValue(key, out var q) && q.Count > 0)
            {
                var pay = q.Dequeue();
                matches.Add(new MatchRow(d.RowNumber, pay.rowNumber, cid, amt));
                matchedDebt.Add(d.RowNumber);
                matchedPay.Add(pay.rowNumber);
            }
        }

        var unmatchedDebt = debts.Where(d => !matchedDebt.Contains(d.RowNumber))
                                 .Select(d => d.RowNumber)
                                 .OrderBy(x => x)
                                 .ToList();

        var unmatchedPay = pays.Where(p => !matchedPay.Contains(p.RowNumber))
                               .Select(p => p.RowNumber)
                               .OrderBy(x => x)
                               .ToList();

        return new PreviewResult(run.Id, matches, unmatchedDebt, unmatchedPay);
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            throw new InvalidOperationException($"Missing required property '{propertyName}'.");

        var result = value.GetString();
        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException($"Property '{propertyName}' is required.");

        return result.Trim();
    }

    private static string? GetOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return null;

        var result = value.GetString();
        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }

    private static decimal GetRequiredDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            throw new InvalidOperationException($"Missing required property '{propertyName}'.");

        if (value.ValueKind == JsonValueKind.Number)
            return value.GetDecimal();

        var raw = value.GetString();
        if (decimal.TryParse(raw, out var parsed))
            return parsed;

        throw new InvalidOperationException($"Property '{propertyName}' must be a valid decimal.");
    }
}