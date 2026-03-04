using FluentValidation;
using ReconciliationApp.API.Contracts;

namespace ReconciliationApp.API.Validation;

public sealed class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
