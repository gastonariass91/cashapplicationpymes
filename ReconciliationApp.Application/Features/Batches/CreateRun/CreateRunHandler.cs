using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Abstractions.Sql;
using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Application.Features.Batches.CreateRun;

public sealed class CreateRunHandler
{
    private readonly IBatchRepository _batches;
    private readonly IBatchRunRepository _runs;
    private readonly IRunNumberService _runNumberService;
    private readonly IUnitOfWork _uow;

    public CreateRunHandler(
        IBatchRepository batches,
        IBatchRunRepository runs,
        IRunNumberService runNumberService,
        IUnitOfWork uow)
    {
        _batches = batches;
        _runs = runs;
        _runNumberService = runNumberService;
        _uow = uow;
    }

    public async Task<CreateRunResult> Handle(CreateRunCommand command, CancellationToken ct)
    {
        var batch = await _batches.GetByIdAsync(command.BatchId, ct);
        if (batch is null)
            throw new InvalidOperationException("Batch not found.");

        var runNumber = await _runNumberService.IncrementAndGetAsync(command.BatchId, ct);

        var run = new BatchRun(command.BatchId, runNumber);

        await _runs.AddAsync(run, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateRunResult(command.BatchId, run.RunNumber, run.CreatedAt);
    }
}
