using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileByCompany;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Entities.Imports;

namespace ReconciliationApp.Application.Features.Imports.UploadCustomersCsv;

public sealed class UploadCustomersCsvHandler
{
    private readonly IImportRowRepository _importRows;
    private readonly IBatchRepository _batches;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;
    private readonly ReconcileByCompanyHandler _reconcileByCompany;
    private readonly ILogger<UploadCustomersCsvHandler> _logger;

    public UploadCustomersCsvHandler(
        IImportRowRepository importRows,
        IBatchRepository batches,
        ICustomerRepository customers,
        IUnitOfWork uow,
        ReconcileByCompanyHandler reconcileByCompany,
        ILogger<UploadCustomersCsvHandler> logger)
    {
        _importRows = importRows;
        _batches = batches;
        _customers = customers;
        _uow = uow;
        _reconcileByCompany = reconcileByCompany;
        _logger = logger;
    }

    public async Task<ImportResult> Handle(Guid batchId, int runNumber, UploadCsvRequest req, CancellationToken ct)
    {
        _logger.LogInformation(
            "Customers CSV import started. BatchId={BatchId} RunNumber={RunNumber}",
            batchId, runNumber);

        var runId = await _importRows.GetRunIdAsync(batchId, runNumber, ct);
        if (runId is null) throw new InvalidOperationException("Run not found.");

        var batch = await _batches.GetByIdAsync(batchId, ct);
        if (batch is null) throw new InvalidOperationException("Batch not found.");

        var jsonRows = CsvSimpleParser.ParseToJsonRows(req.Csv);

        _logger.LogInformation(
            "Customers CSV parsed. BatchId={BatchId} Rows={Rows}",
            batchId, jsonRows.Count);

        await _importRows.DeleteByRunAndTypeAsync(runId.Value, ImportType.Customers, ct);

        var importRows = jsonRows.Select((json, idx) =>
            new ImportRow(runId.Value, ImportType.Customers, idx + 1, json));

        await _importRows.AddRangeAsync(importRows, ct);

        var inserted = 0;
        var updated = 0;
        var errors = new List<ImportErrorDto>();

        for (var i = 0; i < jsonRows.Count; i++)
        {
            var rowNumber = i + 1;

            try
            {
                var row = ParseCustomerRow(jsonRows[i]);

                var existing = await _customers.GetByCompanyAndCustomerKeyAsync(
                    batch.CompanyId,
                    row.CustomerKey,
                    ct);

                if (existing is null)
                {
                    var customer = new Customer(
                        batch.CompanyId,
                        row.CustomerKey,
                        row.Name,
                        row.Email);

                    await _customers.AddAsync(customer, ct);
                    inserted++;
                }
                else
                {
                    existing.UpdateName(row.Name);
                    existing.UpdateEmail(row.Email);
                    updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Customers CSV row error. BatchId={BatchId} RowNumber={RowNumber}",
                    batchId, rowNumber);
                errors.Add(new ImportErrorDto(rowNumber, ex.Message));
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(BuildValidationMessage("customers", errors));

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Customers CSV import completed. BatchId={BatchId} Inserted={Inserted} Updated={Updated}",
            batchId, inserted, updated);

        await _reconcileByCompany.HandleAsync(batch.CompanyId, batchId, runNumber, ct);

        return new ImportResult(
            ImportType: "customers",
            ProcessedCount: jsonRows.Count,
            InsertedCount: inserted,
            UpdatedCount: updated,
            IgnoredCount: 0,
            ClosedCount: 0,
            ErrorCount: 0,
            Errors: Array.Empty<ImportErrorDto>()
        );
    }

    private static CustomerCsvRow ParseCustomerRow(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new CustomerCsvRow(
            CustomerKey: GetRequiredString(root, "customer_key"),
            Name: GetRequiredString(root, "name"),
            Email: GetOptionalString(root, "email")
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

    private static string BuildValidationMessage(string importType, IEnumerable<ImportErrorDto> errors)
    {
        var details = string.Join(" | ", errors.Select(x => $"fila {x.RowNumber}: {x.Message}"));
        return $"Errores al importar {importType}: {details}";
    }

    private sealed record CustomerCsvRow(
        string CustomerKey,
        string Name,
        string? Email
    );
}
