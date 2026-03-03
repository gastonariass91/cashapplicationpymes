using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Batching;

namespace ReconciliationApp.Application.Features.Batches.CreateBatch;

public sealed class CreateBatchHandler
{
    private readonly ICompanyRepository _companies;
    private readonly IBatchRepository _batches;
    private readonly IUnitOfWork _uow;

    public CreateBatchHandler(ICompanyRepository companies, IBatchRepository batches, IUnitOfWork uow)
    {
        _companies = companies;
        _batches = batches;
        _uow = uow;
    }

    public async Task<CreateBatchResult> Handle(CreateBatchCommand command, CancellationToken ct)
    {
        if (command.PeriodTo < command.PeriodFrom)
            throw new ArgumentException("PeriodTo must be >= PeriodFrom");

        var company = await _companies.GetByIdAsync(command.CompanyId, ct);
        if (company is null)
            throw new InvalidOperationException("Company not found.");

        if (await _batches.ExistsForPeriodAsync(command.CompanyId, command.PeriodFrom, command.PeriodTo, ct))
            throw new InvalidOperationException("A batch already exists for this period.");

        var batch = new ReconciliationBatch(command.CompanyId, command.PeriodFrom, command.PeriodTo);

        // MVP: creamos el primer run automáticamente
        batch.CreateNewRun();

        _batches.Add(batch);
        await _uow.SaveChangesAsync(ct);

        return new CreateBatchResult(batch.Id, batch.CompanyId, batch.PeriodFrom, batch.PeriodTo);
    }
}
