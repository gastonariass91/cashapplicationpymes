using System.Text.Json;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Entities.Imports;

namespace ReconciliationApp.Application.Features.Imports.UploadDebtCsv;

public sealed class UploadDebtCsvHandler
{
    private readonly IImportRowRepository _importRows;
    private readonly IBatchRepository _batches;
    private readonly ICustomerRepository _customers;
    private readonly IDebtRepository _debts;
    private readonly IUnitOfWork _uow;

    public UploadDebtCsvHandler(
        IImportRowRepository importRows,
        IBatchRepository batches,
        ICustomerRepository customers,
        IDebtRepository debts,
        IUnitOfWork uow)
    {
        _importRows = importRows;
        _batches = batches;
        _customers = customers;
        _debts = debts;
        _uow = uow;
    }

    public async Task<ImportResult> Handle(Guid batchId, int runNumber, UploadCsvRequest req, CancellationToken ct)
    {
        var runId = await _importRows.GetRunIdAsync(batchId, runNumber, ct);
        if (runId is null) throw new InvalidOperationException("Run not found.");

        var batch = await _batches.GetByIdAsync(batchId, ct);
        if (batch is null) throw new InvalidOperationException("Batch not found.");

        var jsonRows = CsvSimpleParser.ParseToJsonRows(req.Csv);

        await _importRows.DeleteByRunAndTypeAsync(runId.Value, ImportType.Debt, ct);

        var importRows = jsonRows.Select((json, idx) =>
            new ImportRow(runId.Value, ImportType.Debt, idx + 1, json));

        await _importRows.AddRangeAsync(importRows, ct);

        var customersByKey = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);
        var snapshotKeys = new HashSet<(Guid CustomerId, string InvoiceNumber)>();
        var inserted = 0;
        var updated = 0;
        var closed = 0;
        var errors = new List<ImportErrorDto>();

        for (var i = 0; i < jsonRows.Count; i++)
        {
            var rowNumber = i + 1;

            try
            {
                var row = ParseDebtRow(jsonRows[i]);

                if (!customersByKey.TryGetValue(row.CustomerId, out var customer))
                {
                    customer = await _customers.GetByCompanyAndCustomerKeyAsync(batch.CompanyId, row.CustomerId, ct);

                    if (customer is null)
                    {
                        customer = new Customer(
                            batch.CompanyId,
                            row.CustomerId,
                            row.CustomerName,
                            row.CustomerEmail);

                        await _customers.AddAsync(customer, ct);
                    }
                    else
                    {
                        customer.UpdateName(row.CustomerName);
                        customer.UpdateEmail(row.CustomerEmail);
                    }

                    customersByKey[row.CustomerId] = customer;
                }
                else
                {
                    customer.UpdateName(row.CustomerName);
                    customer.UpdateEmail(row.CustomerEmail);
                }

                snapshotKeys.Add((customer.Id, row.InvoiceNumber));

                var existingDebt = await _debts.GetByCompanyCustomerAndInvoiceAsync(
                    batch.CompanyId,
                    customer.Id,
                    row.InvoiceNumber,
                    ct);

                if (existingDebt is null)
                {
                    var debt = new Debt(
                        batch.CompanyId,
                        customer.Id,
                        row.InvoiceNumber,
                        row.IssueDate,
                        row.DueDate,
                        row.Amount,
                        row.Currency,
                        row.OutstandingAmount,
                        runId.Value);

                    await _debts.AddAsync(debt, ct);
                    inserted++;
                }
                else
                {
                    existingDebt.RefreshFromSnapshot(
                        row.IssueDate,
                        row.DueDate,
                        row.Amount,
                        row.Currency,
                        row.OutstandingAmount,
                        runId.Value);

                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ImportErrorDto(rowNumber, ex.Message));
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(BuildValidationMessage("deuda", errors));

        var openDebts = await _debts.ListOpenByCompanyAsync(batch.CompanyId, ct);

        foreach (var openDebt in openDebts)
        {
            var key = (openDebt.CustomerId, openDebt.InvoiceNumber);
            if (!snapshotKeys.Contains(key))
            {
                openDebt.CloseBySnapshot();
                closed++;
            }
        }

        await _uow.SaveChangesAsync(ct);

        return new ImportResult(
            ImportType: "debt",
            ProcessedCount: jsonRows.Count,
            InsertedCount: inserted,
            UpdatedCount: updated,
            IgnoredCount: 0,
            ClosedCount: closed,
            ErrorCount: 0,
            Errors: Array.Empty<ImportErrorDto>()
        );
    }

    private static DebtCsvRow ParseDebtRow(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new DebtCsvRow(
            CustomerId: GetRequiredString(root, "customer_id"),
            CustomerName: GetRequiredString(root, "customer_name"),
            CustomerEmail: GetOptionalString(root, "customer_email"),
            InvoiceNumber: GetRequiredString(root, "invoice_number"),
            IssueDate: DateOnly.Parse(GetRequiredString(root, "issue_date")),
            DueDate: DateOnly.Parse(GetRequiredString(root, "due_date")),
            Amount: GetRequiredDecimal(root, "amount"),
            Currency: GetRequiredString(root, "currency"),
            OutstandingAmount: GetRequiredDecimal(root, "outstanding_amount")
        );
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

    private static string BuildValidationMessage(string importType, IEnumerable<ImportErrorDto> errors)
    {
        var details = string.Join(" | ", errors.Select(x => $"fila {x.RowNumber}: {x.Message}"));
        return $"Errores al importar {importType}: {details}";
    }

    private sealed record DebtCsvRow(
        string CustomerId,
        string CustomerName,
        string? CustomerEmail,
        string InvoiceNumber,
        DateOnly IssueDate,
        DateOnly DueDate,
        decimal Amount,
        string Currency,
        decimal OutstandingAmount
    );
}