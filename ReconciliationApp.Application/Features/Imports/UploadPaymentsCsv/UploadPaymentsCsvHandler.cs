using System.Text.Json;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Entities.Imports;

namespace ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv;

public sealed class UploadPaymentsCsvHandler
{
    private readonly IImportRowRepository _importRows;
    private readonly IBatchRepository _batches;
    private readonly ICustomerRepository _customers;
    private readonly IPaymentRepository _payments;
    private readonly IUnitOfWork _uow;

    public UploadPaymentsCsvHandler(
        IImportRowRepository importRows,
        IBatchRepository batches,
        ICustomerRepository customers,
        IPaymentRepository payments,
        IUnitOfWork uow)
    {
        _importRows = importRows;
        _batches = batches;
        _customers = customers;
        _payments = payments;
        _uow = uow;
    }

    public async Task Handle(Guid batchId, int runNumber, UploadCsvRequest req, CancellationToken ct)
    {
        var runId = await _importRows.GetRunIdAsync(batchId, runNumber, ct);
        if (runId is null) throw new InvalidOperationException("Run not found.");

        var batch = await _batches.GetByIdAsync(batchId, ct);
        if (batch is null) throw new InvalidOperationException("Batch not found.");

        var jsonRows = CsvSimpleParser.ParseToJsonRows(req.Csv);

        await _importRows.DeleteByRunAndTypeAsync(runId.Value, ImportType.Payments, ct);

        var importRows = jsonRows.Select((json, idx) =>
            new ImportRow(runId.Value, ImportType.Payments, idx + 1, json));

        await _importRows.AddRangeAsync(importRows, ct);

        foreach (var json in jsonRows)
        {
            var row = ParsePaymentRow(json);

            var existingPayment = await _payments.GetByCompanyAndPaymentNumberAsync(
                batch.CompanyId,
                row.PaymentNumber,
                ct);

            if (existingPayment is not null)
            {
                continue;
            }

            Guid? customerId = null;

            if (!string.IsNullOrWhiteSpace(row.PayerTaxId))
            {
                var customer = await _customers.GetByCompanyAndCustomerKeyAsync(
                    batch.CompanyId,
                    row.PayerTaxId,
                    ct);

                customerId = customer?.Id;
            }

            var payment = new Payment(
                batch.CompanyId,
                row.PaymentNumber,
                row.PaymentDate,
                row.AccountNumber,
                row.Amount,
                row.Currency,
                customerId,
                row.PayerTaxId,
                runId.Value);

            await _payments.AddAsync(payment, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }

    private static PaymentCsvRow ParsePaymentRow(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new PaymentCsvRow(
            PaymentNumber: GetRequiredString(root, "payment_number"),
            PaymentDate: DateOnly.Parse(GetRequiredString(root, "payment_date")),
            AccountNumber: GetRequiredString(root, "account_number"),
            PayerTaxId: GetOptionalString(root, "payer_tax_id"),
            Amount: GetRequiredDecimal(root, "amount"),
            Currency: GetRequiredString(root, "currency")
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

    private sealed record PaymentCsvRow(
        string PaymentNumber,
        DateOnly PaymentDate,
        string AccountNumber,
        string? PayerTaxId,
        decimal Amount,
        string Currency
    );
}