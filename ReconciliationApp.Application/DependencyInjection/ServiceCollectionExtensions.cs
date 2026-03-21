using Microsoft.Extensions.DependencyInjection;
using ReconciliationApp.Application.Features.Batches.CreateBatch;
using ReconciliationApp.Application.Features.Batches.CreateRun;
using ReconciliationApp.Application.Features.Companies.CreateCompany;
using ReconciliationApp.Application.Features.Imports.UploadCustomersCsv;
using ReconciliationApp.Application.Features.Imports.UploadDebtCsv;
using ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileByCompany;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileResult;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileRun;
using ReconciliationApp.Application.Features.Auth.Login;
using ReconciliationApp.Application.Features.Auth.Register;

namespace ReconciliationApp.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Reconciliation
        services.AddScoped<ReconcileRunHandler>();
        services.AddScoped<ReconcileResultHandler>();
        services.AddScoped<ReconcileByCompanyHandler>(); // orquesta auto-reconcile post-import

        // Batches
        services.AddScoped<CreateBatchHandler>();
        services.AddScoped<CreateRunHandler>();

        // Companies
        services.AddScoped<CreateCompanyHandler>();

        // Imports — cada uno dispara ReconcileByCompanyHandler al final
        services.AddScoped<UploadCustomersCsvHandler>();
        services.AddScoped<UploadDebtCsvHandler>();
        services.AddScoped<UploadPaymentsCsvHandler>();

        // Auth
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterUserHandler>();

        return services;
    }
}
