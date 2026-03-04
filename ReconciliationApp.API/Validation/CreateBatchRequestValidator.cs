using FluentValidation;
using ReconciliationApp.API.Contracts;

namespace ReconciliationApp.API.Validation;

public sealed class CreateBatchRequestValidator : AbstractValidator<CreateBatchRequest>
{
    public CreateBatchRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.PeriodFrom).LessThanOrEqualTo(x => x.PeriodTo);
    }
}
