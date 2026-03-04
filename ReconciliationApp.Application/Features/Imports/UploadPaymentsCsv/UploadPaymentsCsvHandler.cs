using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Imports;

namespace ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv;

public sealed class UploadPaymentsCsvHandler
{
    private readonly IImportRowRepository _repo;
    private readonly IUnitOfWork _uow;

    public UploadPaymentsCsvHandler(IImportRowRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(Guid batchId, int runNumber, UploadCsvRequest req, CancellationToken ct)
    {
        var runId = await _repo.GetRunIdAsync(batchId, runNumber, ct);
        if (runId is null) throw new InvalidOperationException("Run not found.");

        var jsonRows = CsvSimpleParser.ParseToJsonRows(req.Csv);

        await _repo.DeleteByRunAndTypeAsync(runId.Value, ImportType.Payments, ct);

        var rows = jsonRows.Select((json, idx) =>
            new ImportRow(runId.Value, ImportType.Payments, idx + 1, json));

        await _repo.AddRangeAsync(rows, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
