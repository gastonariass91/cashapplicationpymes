using System.Text.Json;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Entities.Imports;

namespace ReconciliationApp.Application.Features.Imports.UploadCustomersCsv;

public sealed class UploadCustomersCsvHandler
{
    private readonly IImportRowRepository _importRows;
    private readonly IBatchRepository _batches;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;

    public UploadCustomersCsvHandler(
        IImportRowRepository importRows,
        IBatchRepository batches,
        ICustomerRepository customers,
        IUnitOfWork uow)
    {
        _importRows = importRows;
        _batches = batches;
        _customers = customers;
        _uow = uow;
    }

    public async Task Handle(Guid batchId, int runNumber, UploadCsvRequest req, CancellationToken ct)
    {
        var runId = await _importRows.GetRunIdAsync(batchId, runNumber, ct);
        if (runId is null) throw new InvalidOperationException("Run not found.");

        var batch = await _batches.GetByIdAsync(batchId, ct);
        if (batch is null) throw new InvalidOperationException("Batch not found.");

        var jsonRows = CsvSimpleParser.ParseToJsonRows(req.Csv);

        await _importRows.DeleteByRunAndTypeAsync(runId.Value, ImportType.Customers, ct);

        var importRows = jsonRows.Select((json, idx) =>
            new ImportRow(runId.Value, ImportType.Customers, idx + 1, json));

        await _importRows.AddRangeAsync(importRows, ct);

        foreach (var json in jsonRows)
        {
            var row = ParseCustomerRow(json);

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
            }
            else
            {
                existing.UpdateName(row.Name);
                existing.UpdateEmail(row.Email);
            }
        }

        await _uow.SaveChangesAsync(ct);
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

    private sealed record CustomerCsvRow(
        string CustomerKey,
        string Name,
        string? Email
    );
}