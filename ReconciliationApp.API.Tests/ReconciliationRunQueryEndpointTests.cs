using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ReconciliationApp.Domain.Entities.Batching;
using ReconciliationApp.Domain.Entities.ReconciliationReview;

namespace ReconciliationApp.API.Tests;

/// <summary>
/// Tests de integración para los endpoints de review de conciliación.
/// Usan WebApplicationFactory con repositorio in-memory — no necesitan DB real.
/// </summary>
public sealed class ReconciliationRunQueryEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    private readonly InMemoryReconciliationReviewRepository _repo;

    // IDs fijos para todos los tests
    private static readonly Guid CompanyId = Guid.Parse("77e3c972-1323-40c2-ba1e-044723bc4c00");
    private static readonly Guid BatchRunId = Guid.NewGuid();

    public ReconciliationRunQueryEndpointTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
        _repo = factory.ReviewRepo;
        _repo.Clear();
    }

    // -----------------------------------------------------------------------
    // GET /api/companies/{companyId}/reconciliation/current
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetCurrent_Returns200_WhenRunExists()
    {
        _repo.Seed(MakeRun(BatchRunId, withCases: true));

        var response = await _client.GetAsync($"/api/companies/{CompanyId}/reconciliation/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrent_Returns404_WhenNoRunExists()
    {
        // repo vacío
        var response = await _client.GetAsync($"/api/companies/{CompanyId}/reconciliation/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrent_ReturnsCases_WhenRunHasCases()
    {
        _repo.Seed(MakeRun(BatchRunId, withCases: true));

        var response = await _client.GetAsync($"/api/companies/{CompanyId}/reconciliation/current");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var cases = body.GetProperty("cases");
        Assert.True(cases.GetArrayLength() > 0);
    }

    // -----------------------------------------------------------------------
    // GET /api/reconciliation-runs/{runId}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetRun_Returns200_WhenExists()
    {
        _repo.Seed(MakeRun(BatchRunId, withCases: true));

        var response = await _client.GetAsync($"/api/reconciliation-runs/{BatchRunId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRun_Returns404_WhenNotExists()
    {
        var response = await _client.GetAsync($"/api/reconciliation-runs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // POST /api/reconciliation-runs/{runId}/cases/{caseId}/accept
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AcceptCase_Returns200_WhenCaseExists()
    {
        var run = MakeRun(BatchRunId, withCases: true);
        _repo.Seed(run);
        var caseId = run.Cases.First().CaseId;

        var response = await _client.PostAsync(
            $"/api/reconciliation-runs/{BatchRunId}/cases/{caseId}/accept", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AcceptCase_Returns404_WhenRunNotExists()
    {
        var response = await _client.PostAsync(
            $"/api/reconciliation-runs/{Guid.NewGuid()}/cases/case-1/accept", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AcceptCase_ChangesStatusToOk()
    {
        var run = MakeRun(BatchRunId, withCases: true);
        _repo.Seed(run);
        var targetCase = run.Cases.First(c => c.Status == "pending");

        await _client.PostAsync(
            $"/api/reconciliation-runs/{BatchRunId}/cases/{targetCase.CaseId}/accept", null);

        Assert.Equal("ok", targetCase.Status);
    }

    // -----------------------------------------------------------------------
    // POST /api/reconciliation-runs/{runId}/cases/{caseId}/exception
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkException_Returns200_WhenCaseExists()
    {
        var run = MakeRun(BatchRunId, withCases: true);
        _repo.Seed(run);
        var caseId = run.Cases.First().CaseId;

        var response = await _client.PostAsync(
            $"/api/reconciliation-runs/{BatchRunId}/cases/{caseId}/exception", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarkException_ChangesStatusToException()
    {
        var run = MakeRun(BatchRunId, withCases: true);
        _repo.Seed(run);
        var targetCase = run.Cases.First();

        await _client.PostAsync(
            $"/api/reconciliation-runs/{BatchRunId}/cases/{targetCase.CaseId}/exception", null);

        Assert.Equal("exception", targetCase.Status);
    }

    // -----------------------------------------------------------------------
    // POST /api/reconciliation-runs/{runId}/confirm
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Confirm_Returns200_WhenNoPendingCases()
    {
        var run = MakeRun(BatchRunId, withCases: false); // sin pendientes
        _repo.Seed(run);

        var response = await _client.PostAsync(
            $"/api/reconciliation-runs/{BatchRunId}/confirm", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Returns400_WhenHasPendingCases()
    {
        var run = MakeRun(BatchRunId, withCases: true); // tiene pendientes
        _repo.Seed(run);

        var response = await _client.PostAsync(
            $"/api/reconciliation-runs/{BatchRunId}/confirm", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Returns404_WhenRunNotExists()
    {
        var response = await _client.PostAsync(
            $"/api/reconciliation-runs/{Guid.NewGuid()}/confirm", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_SetsStatusToConfirmed_WhenAllCasesResolved()
    {
        var run = MakeRun(BatchRunId, withCases: false);
        _repo.Seed(run);

        await _client.PostAsync($"/api/reconciliation-runs/{BatchRunId}/confirm", null);

        Assert.Equal("confirmed", run.Status);
    }

    // -----------------------------------------------------------------------
    // POST /api/reconciliation-runs/{runId}/cases/bulk-accept
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BulkAccept_Returns200_WhenRunExists()
    {
        var run = MakeRun(BatchRunId, withCases: true);
        _repo.Seed(run);
        var caseIds = run.Cases.Select(c => c.CaseId).ToList();

        var response = await _client.PostAsJsonAsync(
            $"/api/reconciliation-runs/{BatchRunId}/cases/bulk-accept",
            new { caseIds });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BulkAccept_AcceptsAllSelectedCases()
    {
        var run = MakeRun(BatchRunId, withCases: true);
        _repo.Seed(run);
        var caseIds = run.Cases.Select(c => c.CaseId).ToList();

        await _client.PostAsJsonAsync(
            $"/api/reconciliation-runs/{BatchRunId}/cases/bulk-accept",
            new { caseIds });

        Assert.All(run.Cases, c => Assert.Equal("ok", c.Status));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static ReconciliationRun MakeRun(Guid batchRunId, bool withCases)
    {
        // Construimos el grafo BatchRun → Batch mínimo para que el repositorio funcione
        var batch = new ReconciliationBatch(
            CompanyId,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31));

        var batchRun = batch.CreateNewRun();

        // Forzamos el Id del BatchRun via reflection para que coincida con batchRunId
        typeof(BatchRun)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(batchRun, batchRunId);
            typeof(BatchRun)
                .GetProperty("Batch", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
                .SetValue(batchRun, batch);

        var run = new ReconciliationRun(batchRunId, batchRunId.ToString());

        // Forzamos la navegación BatchRun
        typeof(ReconciliationRun)
            .GetProperty("BatchRun", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(run, batchRun);

        if (withCases)
        {
            run.Cases.Add(MakeCase(run.Id, "case-1", "pending"));
            run.Cases.Add(MakeCase(run.Id, "case-2", "ok"));
        }

        return run;
    }

    private static ReconciliationCase MakeCase(Guid runId, string caseId, string status)
    {
        var c = new ReconciliationCase(
            runId, caseId,
            debtRow: 1, paymentRow: 1,
            customer: "CUST-01",
            debtAmount: 1000m, paymentAmount: 1000m, delta: 0m,
            rule: "Cliente+Monto", status: status,
            confidence: "high", matchType: "exact",
            evidence: "Mismo cliente · monto exacto",
            suggestion: "Aceptar");

        return c;
    }
}
