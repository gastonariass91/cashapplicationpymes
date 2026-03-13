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

    public async Task Handle(Guid batchId, int runNumber, UploadCsvRequest req, CancellationToken ct)
    {
        var runId = await _importRows.GetRunIdAsync(batchId, runNumber, ct);
        if (runId is null) throw new InvalidOperationException("Run not found.");

        var batch = await _batches.GetByIdAsync(batchId, ct);
        if (batch is null) throw new InvalidOperationException("Batch not found.");

        var jsonRows = CsvSimpleParser.ParseToJsonRows(req.Csv);

        await _importRows.DeleteByRunAndTypeAsync(runId.Value, ImportType.Debt, ct);
        await _debts.DeleteBySourceBatchRunIdAsync(runId.Value, ct);

        var importRows = jsonRows.Select((json, idx) =>
            new ImportRow(runId.Value, ImportType.Debt, idx + 1, json));

        await _importRows.AddRangeAsync(importRows, ct);

        var debtsToInsert = new List<Debt>();

        // Cache en memoria para no duplicar customers dentro del mismo CSV
        var customersByKey = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);

        foreach (var json in jsonRows)
        {
            var row = ParseDebtRow(json);

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
                // Si ya fue creado/encontrado en esta corrida, igual actualizamos datos por si vinieron distintos
                customer.UpdateName(row.CustomerName);
                customer.UpdateEmail(row.CustomerEmail);
            }

            debtsToInsert.Add(new Debt(
                batch.CompanyId,
                customer.Id,
                row.InvoiceNumber,
                row.IssueDate,
                row.DueDate,
                row.Amount,
                row.Currency,
                row.OutstandingAmount,
                runId.Value));
        }

        await _debts.AddRangeAsync(debtsToInsert, ct);
        await _uow.SaveChangesAsync(ct);
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