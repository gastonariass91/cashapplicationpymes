using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Entities.ReconciliationReview;

namespace ReconciliationApp.API.Tests;

/// <summary>
/// Levanta la API en memoria reemplazando IReconciliationReviewRepository
/// con un stub in-memory. No necesita DB real ni conexión externa.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public InMemoryReconciliationReviewRepository ReviewRepo { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Reemplazamos toda la infra con stubs
            services.RemoveAll<IReconciliationReviewRepository>();
            services.AddSingleton<IReconciliationReviewRepository>(ReviewRepo);

            // Deshabilitamos health checks de postgres para no necesitar DB
            services.RemoveAll<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration>();
        });

        builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=test;Username=test;Password=test");
    }
}

// ---------------------------------------------------------------------------
// Stub in-memory del repositorio de review
// ---------------------------------------------------------------------------

public sealed class InMemoryReconciliationReviewRepository : IReconciliationReviewRepository
{
    private readonly List<ReconciliationRun> _runs = new();

    public void Seed(ReconciliationRun run) => _runs.Add(run);

    public void Clear() => _runs.Clear();

    public Task<ReconciliationRun?> GetRunAsync(string runId, CancellationToken ct = default)
        => Task.FromResult(_runs.FirstOrDefault(r => r.BatchRunId.ToString() == runId));

    public Task<ReconciliationRun?> GetCurrentRunAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult(_runs.OrderByDescending(r => r.CreatedAt).FirstOrDefault());

    public Task<ReviewRunTotals?> GetRunTotalsAsync(string runId, CancellationToken ct = default)
        => Task.FromResult<ReviewRunTotals?>(new ReviewRunTotals(5, 4, 10_000m, 8_500m));

    public Task<ReviewRunTotals?> GetCurrentRunTotalsAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult<ReviewRunTotals?>(new ReviewRunTotals(5, 4, 10_000m, 8_500m));

    public Task<string?> GetRunCompanyNameAsync(string runId, CancellationToken ct = default)
        => Task.FromResult<string?>("Test Company SA");

    public Task<string?> GetCompanyNameAsync(Guid companyId, CancellationToken ct = default)
        => Task.FromResult<string?>("Test Company SA");

    public Task<bool> AcceptCaseAsync(string runId, string caseId, CancellationToken ct = default)
    {
        var run = _runs.FirstOrDefault(r => r.BatchRunId.ToString() == runId);
        if (run is null) return Task.FromResult(false);

        var item = run.Cases.FirstOrDefault(c => c.CaseId == caseId);
        if (item is null) return Task.FromResult(false);

        item.Accept();
        return Task.FromResult(true);
    }

    public Task<bool> MarkExceptionAsync(string runId, string caseId, CancellationToken ct = default)
    {
        var run = _runs.FirstOrDefault(r => r.BatchRunId.ToString() == runId);
        if (run is null) return Task.FromResult(false);

        var item = run.Cases.FirstOrDefault(c => c.CaseId == caseId);
        if (item is null) return Task.FromResult(false);

        item.MarkException();
        return Task.FromResult(true);
    }

    public Task<int> BulkAcceptAsync(string runId, IEnumerable<string> caseIds, CancellationToken ct = default)
    {
        var run = _runs.FirstOrDefault(r => r.BatchRunId.ToString() == runId);
        if (run is null) return Task.FromResult(0);

        var ids = caseIds.ToHashSet();
        var items = run.Cases.Where(c => ids.Contains(c.CaseId)).ToList();
        foreach (var item in items) item.Accept();
        return Task.FromResult(items.Count);
    }

    public Task<(bool CanConfirm, string Status)> ConfirmAsync(string runId, CancellationToken ct = default)
    {
        var run = _runs.FirstOrDefault(r => r.BatchRunId.ToString() == runId);
        if (run is null) return Task.FromResult((false, "in_review"));

        var hasPending = run.Cases.Any(c => c.Status == "pending");
        var hasException = run.Cases.Any(c => c.Status == "exception");

        if (hasPending || hasException)
            return Task.FromResult((false, run.Status));

        run.Confirm();
        return Task.FromResult((true, run.Status));
    }
}
