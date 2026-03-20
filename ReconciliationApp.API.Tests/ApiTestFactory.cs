using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.Core;
using ReconciliationApp.Domain.Entities.ReconciliationReview;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.API.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public InMemoryReconciliationReviewRepository ReviewRepo { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", "test-secret-key-for-integration-tests-only-32chars");
        builder.UseSetting("Jwt:Issuer", "ReconciliationApp");
        builder.UseSetting("Jwt:Audience", "ReconciliationApp");
        builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=test;Username=test;Password=test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IReconciliationReviewRepository>();
            services.AddSingleton<IReconciliationReviewRepository>(ReviewRepo);

            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository>(new StubUserRepository());

            services.RemoveAll<ICompanyRepository>();
            services.AddSingleton<ICompanyRepository>(new StubCompanyRepository());

            services.RemoveAll<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration>();

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("TestDb"));

            // Reemplazar auth con esquema de test que siempre autentica
            services.AddAuthentication(defaultScheme: "Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.AddAuthorization(options =>
            {
                var testPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes("Test")
                    .Build();

                options.DefaultPolicy = testPolicy;
                options.FallbackPolicy = testPolicy;
                options.AddPolicy("AdminOnly", testPolicy);
            });
        });
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

// ---------------------------------------------------------------------------
// Stubs de repositorios
// ---------------------------------------------------------------------------

file sealed class StubUserRepository : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult<User?>(null);
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<User?>(null);
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(false);
    public Task AddAsync(User user, CancellationToken ct = default)
        => Task.CompletedTask;
}

file sealed class StubCompanyRepository : ICompanyRepository
{
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Company?>(null);
    public void Add(Company company) { }
    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => Task.FromResult(false);
}

// ---------------------------------------------------------------------------
// Handler de autenticación de test — siempre autentica como Admin
// ---------------------------------------------------------------------------

file sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("sub",        Guid.NewGuid().ToString()),
            new Claim("email",      "test@test.com"),
            new Claim("company_id", "77e3c972-1323-40c2-ba1e-044723bc4c00"),
            new Claim(ClaimTypes.Role, "Admin"),
        };

        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
