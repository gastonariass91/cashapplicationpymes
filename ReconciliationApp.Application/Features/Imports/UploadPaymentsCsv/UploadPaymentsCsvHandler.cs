using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileByCompany;
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
    private readonly ReconcileByCompanyHandler _reconcileByCompany;
    private readonly ILogger<UploadPaymentsCsvHandler> _logger;

    public UploadPaymentsCsvHandler(
        IImportRowRepository importRows,
        IBatchRepository batches,
        ICustomerRepository customers,
        IPaymentRepository payments,
        IUnitOfWork uow,
        ReconcileByCompanyHandler reconcileByCompany,
        ILogger<UploadPaymentsCsvHandler> logger)
    {
        _importRows = importRows;
        _batches = batches;
        _customers = customers;
        _payments = payments;
        _uow = uow;
        _reconcileByCompany = reconcileByCompany;
        _logger = logger;
    }

    public async Task<ImportResult> Handle(Guid batchId, int runNumber, UploadCsvRequest req, CancellationToken ct)
    {
        _logger.LogInformation(
            "Payments CSV import started. BatchId={BatchId} RunNumber={RunNumber}",
            batchId, runNumber);

        var runId = await _importRows.GetRunIdAsync(batchId, runNumber, ct);
        if (runId is null) throw new InvalidOperationException("Run not found.");

        var batch = await _batches.GetByIdAsync(batchId, ct);
        if (batch is null) throw new InvalidOperationException("Batch not found.");

        var jsonRows = CsvSimpleParser.ParseToJsonRows(req.Csv);

        _logger.LogInformation(
            "Payments CSV parsed. BatchId={BatchId} Rows={Rows}",
            batchId, jsonRows.Count);

        await _importRows.DeleteByRunAndTypeAsync(runId.Value, ImportType.Payments, ct);

        var importRows = jsonRows.Select((json, idx) =>
            new ImportRow(runId.Value, ImportType.Payments, idx + 1, json));

        await _importRows.AddRangeAsync(importRows, ct);

        var inserted = 0;
        var ignored = 0;
        var errors = new List<ImportErrorDto>();

        for (var i = 0; i < jsonRows.Count; i++)
        {
            var rowNumber = i + 1;

            try
            {
                var row = ParsePaymentRow(jsonRows[i]);

                var existingPayment = await _payments.GetByCompanyAndPaymentNumberAsync(
                    batch.CompanyId,
                    row.PaymentNumber,
                    ct);

                if (existingPayment is not null)
                {
                    ignored++;
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
                inserted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Payments CSV row error. BatchId={BatchId} RowNumber={RowNumber}",
                    batchId, rowNumber);
                errors.Add(new ImportErrorDto(rowNumber, ex.Message));
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(BuildValidationMessage("pagos", errors));

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payments CSV import completed. BatchId={BatchId} Inserted={Inserted} Ignored={Ignored}",
            batchId, inserted, ignored);

        await _reconcileByCompany.HandleAsync(batch.CompanyId, batchId, runNumber, ct);

        return new ImportResult(
            ImportType: "payments",
            ProcessedCount: jsonRows.Count,
            InsertedCount: inserted,
            UpdatedCount: 0,
            IgnoredCount: ignored,
            ClosedCount: 0,
            ErrorCount: 0,
            Errors: Array.Empty<ImportErrorDto>()
        );
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

    private static string BuildValidationMessage(string importType, IEnumerable<ImportErrorDto> errors)
    {
        var details = string.Join(" | ", errors.Select(x => $"fila {x.RowNumber}: {x.Message}"));
        return $"Errores al importar {importType}: {details}";
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
