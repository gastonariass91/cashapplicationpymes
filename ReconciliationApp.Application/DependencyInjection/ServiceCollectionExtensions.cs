using Microsoft.Extensions.DependencyInjection;

namespace ReconciliationApp.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Acá más adelante registramos cosas de Application:
        // - MediatR (si lo sumamos)
        // - Validators (FluentValidation)
        // - Behaviors, etc.
        return services;
    }
}
