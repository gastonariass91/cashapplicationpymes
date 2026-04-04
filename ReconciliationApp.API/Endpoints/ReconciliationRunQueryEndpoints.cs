using Microsoft.AspNetCore.Http;
using ReconciliationApp.API.Contracts.Reconciliation;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.ReconciliationReview;

namespace ReconciliationApp.API.Endpoints;

public static class ReconciliationRunQueryEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationRunQueryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reconciliation-runs/{runId}", async (string runId, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var run = await repo.GetRunAsync(runId, ct);
            if (run is null) return Results.NotFound();

            var totals = await repo.GetRunTotalsAsync(runId, ct);
            if (totals is null) return Results.NotFound();

            var companyName = await repo.GetRunCompanyNameAsync(runId, ct);
            if (string.IsNullOrWhiteSpace(companyName)) return Results.NotFound();

            var dto = ToDto(runId, run, totals, companyName);
            return Results.Ok(dto);
        })
        .WithName("GetReconciliationRun")
        .WithTags("Reconciliation Runs")
        .Produces<ReconciliationRunDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/companies/{companyId:guid}/reconciliation/current", async (
            Guid companyId,
            IReconciliationReviewRepository repo,
            CancellationToken ct) =>
        {
            var run = await repo.GetCurrentRunAsync(companyId, ct);
            if (run is null) return Results.NotFound();

            var totals = await repo.GetCurrentRunTotalsAsync(companyId, ct);
            if (totals is null) return Results.NotFound();

            var companyName = await repo.GetCompanyNameAsync(companyId, ct);
            if (string.IsNullOrWhiteSpace(companyName)) return Results.NotFound();

            var dto = ToDto(run.BatchRunId.ToString(), run, totals, companyName);
            return Results.Ok(dto);
        })
        .WithName("GetCurrentReconciliationRun")
        .WithTags("Reconciliation Runs")
        .Produces<ReconciliationRunDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/{caseId}/accept", async (string runId, string caseId, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var ok = await repo.AcceptCaseAsync(runId, caseId, ct);
            if (!ok) return Results.NotFound();

            return Results.Ok(new ActionResponseDto(true, "Caso aceptado correctamente", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/{caseId}/exception", async (string runId, string caseId, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var ok = await repo.MarkExceptionAsync(runId, caseId, ct);
            if (!ok) return Results.NotFound();

            return Results.Ok(new ActionResponseDto(true, "Caso marcado como excepción", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/bulk-accept", async (string runId, BulkAcceptRequest request, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var run = await repo.GetRunAsync(runId, ct);
            if (run is null) return Results.NotFound();

            await repo.BulkAcceptAsync(runId, request.CaseIds, ct);
            return Results.Ok(new ActionResponseDto(true, "Casos aceptados correctamente", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/reconciliation-runs/{runId}/confirm", async (string runId, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var run = await repo.GetRunAsync(runId, ct);
            if (run is null) return Results.NotFound();

            var result = await repo.ConfirmAsync(runId, ct);
            if (!result.CanConfirm)
            {
                return Results.BadRequest(new ActionResponseDto(
                    false,
                    "No se puede confirmar: todavía hay pendientes o excepciones.",
                    result.Status
                ));
            }

            return Results.Ok(new ActionResponseDto(true, "Conciliación confirmada", "confirmed"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK)
        .Produces<ActionResponseDto>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static ReconciliationRunDto ToDto(string runId, ReconciliationRun run, ReviewRunTotals totals, string companyName)
    {
        var orderedCases = run.Cases
            .OrderBy(x => x.DebtRowNumber)
            .ThenBy(x => x.PaymentRowNumber)
            .ToList();

        var cases = orderedCases
            .Select(x => new ReconciliationCaseDto(
                x.CaseId,
                x.DebtRowNumber ?? 0,
                x.PaymentRowNumber ?? 0,
                x.Customer,
                x.DebtAmount,
                x.PaymentAmount,
                x.Delta,
                x.Rule,
                x.Status,
                x.Confidence,
                x.MatchType,
                x.Evidence,
                x.Suggestion,
                x.ResolvedBy))
            .ToList();

        var totalCases = cases.Count;
        var resolvedCases = cases.Count(c => c.Status is "ok" or "exception");
        var automaticCases = cases.Count(c => c.Status == "ok");
        var pendingCases = cases.Count(c => c.Status == "pending");
        var exceptionCases = cases.Count(c => c.Status == "exception");

        var companyId = run.BatchRun.Batch.CompanyId.ToString();
        var period = BuildPeriodLabel(run);

        var summary = new RunSummaryDto(
            RunId: runId,
            CompanyId: companyId,
            CompanyName: companyName,
            Period: period,
            Status: run.Status,
            TotalCases: totalCases,
            ResolvedCases: resolvedCases,
            AutomaticCases: automaticCases,
            PendingCases: pendingCases,
            ExceptionCases: exceptionCases,
            DebtsRowsTotal: totals.DebtsRowsTotal,
            PaymentsRowsTotal: totals.PaymentsRowsTotal,
            DebtsAmountTotal: totals.DebtsAmountTotal,
            PaymentsAmountTotal: totals.PaymentsAmountTotal
        );

        return new ReconciliationRunDto(summary, cases);
    }

    private static string BuildPeriodLabel(ReconciliationRun run)
    {
        var from = run.BatchRun.Batch.PeriodFrom;
        var to = run.BatchRun.Batch.PeriodTo;

        if (from.Year == to.Year && from.Month == to.Month)
            return $"{from.Year:D4}-{from.Month:D2}";

        return $"{from:yyyy-MM-dd} a {to:yyyy-MM-dd}";
    }

    public sealed record BulkAcceptRequest(List<string> CaseIds);
}