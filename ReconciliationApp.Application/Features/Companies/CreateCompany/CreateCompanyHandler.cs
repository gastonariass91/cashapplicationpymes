using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Features.Companies.CreateCompany;

public sealed class CreateCompanyHandler
{
    private readonly ICompanyRepository _companies;
    private readonly IUnitOfWork _uow;

    public CreateCompanyHandler(ICompanyRepository companies, IUnitOfWork uow)
    {
        _companies = companies;
        _uow = uow;
    }

    public async Task<CreateCompanyResult> Handle(CreateCompanyCommand command, CancellationToken ct)
    {
        var name = command.Name?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required");

        // Simple guard (MVP). Luego hacemos manejo de errores más prolijo.
        if (await _companies.ExistsByNameAsync(name, ct))
            throw new InvalidOperationException("A company with this name already exists.");

        var company = new Company(name);

        _companies.Add(company);
        await _uow.SaveChangesAsync(ct);

        return new CreateCompanyResult(company.Id, company.Name);
    }
}
