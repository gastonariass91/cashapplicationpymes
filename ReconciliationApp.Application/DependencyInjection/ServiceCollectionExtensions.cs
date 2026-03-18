using Microsoft.Extensions.DependencyInjection;
using ReconciliationApp.Application.Features.Batches.CreateBatch;
using ReconciliationApp.Application.Features.Batches.CreateRun;
using ReconciliationApp.Application.Features.Companies.CreateCompany;
using ReconciliationApp.Application.Features.Imports.UploadDebtCsv;
using ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileResult;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileRun;

namespace ReconciliationApp.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Handlers registrados acá, no en Program.cs
        // Cuando agregues un handler nuevo, solo registralo acá.
        services.AddScoped<CreateCompanyHandler>();
        services.AddScoped<CreateBatchHandler>();
        services.AddScoped<CreateRunHandler>();
        services.AddScoped<UploadDebtCsvHandler>();
        services.AddScoped<UploadPaymentsCsvHandler>();
        services.AddScoped<ReconcileRunHandler>();
        services.AddScoped<ReconcileResultHandler>();

        // TODO: si en el futuro adoptás MediatR, reemplazá el bloque de arriba por:
        // services.AddMediatR(cfg =>
        //     cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        return services;
    }
}
