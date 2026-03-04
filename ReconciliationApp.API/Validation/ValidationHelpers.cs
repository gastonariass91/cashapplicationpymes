using FluentValidation;
using FluentValidation.Results;

namespace ReconciliationApp.API.Validation;

public static class ValidationHelpers
{
    public static async Task<IResult?> ValidateAsync<T>(T model, IServiceProvider sp, CancellationToken ct)
    {
        var validator = sp.GetService<IValidator<T>>();
        if (validator is null) return null; // no validator => no validation

        ValidationResult result = await validator.ValidateAsync(model, ct);
        if (result.IsValid) return null;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
