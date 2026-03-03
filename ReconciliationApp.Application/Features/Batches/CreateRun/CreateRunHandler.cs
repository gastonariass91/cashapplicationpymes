using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;

namespace ReconciliationApp.Application.Features.Batches.CreateRun;

public sealed class CreateRunHandler
{
    private readonly IBatchRepository _batches;
    private readonly IUnitOfWork _uow;

    public CreateRunHandler(IBatchRepository batches, IUnitOfWork uow)
    {
        _batches = batches;
        _uow = uow;
    }

    public async Task<CreateRunResult> Handle(CreateRunCommand command, CancellationToken ct)
    {
        var batch = await _batches.GetByIdAsync(command.BatchId, ct);
        if (batch is null)
            throw new InvalidOperationException("Batch not found.");

        var run = batch.CreateNewRun();
        await _uow.SaveChangesAsync(ct);

        return new CreateRunResult(batch.Id, run.RunNumber, run.CreatedAt);
    }
}
