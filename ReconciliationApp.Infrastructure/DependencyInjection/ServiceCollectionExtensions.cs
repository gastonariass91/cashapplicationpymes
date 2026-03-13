using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Abstractions.Sql;
using ReconciliationApp.Infrastructure.Persistence;
using ReconciliationApp.Infrastructure.Repositories;
using ReconciliationApp.Infrastructure.Sql;

namespace ReconciliationApp.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Missing ConnectionStrings:Default in configuration.");

        services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(cs));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<ICompanyRepository, EfCompanyRepository>();
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<IDebtRepository, EfDebtRepository>();
        services.AddScoped<IPaymentRepository, EfPaymentRepository>();
        services.AddScoped<IBatchRepository, EfBatchRepository>();
        services.AddScoped<IBatchRunRepository, EfBatchRunRepository>();
        services.AddScoped<IImportRowRepository, EfImportRowRepository>();
        services.AddScoped<IReconciliationMatchRepository, EfReconciliationMatchRepository>();
        services.AddScoped<IReconciliationCaseRepository, EfReconciliationCaseRepository>();
        services.AddScoped<IReconciliationReviewRepository, EfReconciliationReviewRepository>();

        services.AddScoped<IRunNumberService, RunNumberService>();

        return services;
    }
}
