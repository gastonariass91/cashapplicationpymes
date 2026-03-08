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
            var ok = await repo.SeedRunIfMissingAsync(runId, ct);
            if (!ok) return Results.NotFound();

            var run = await repo.GetRunAsync(runId, ct);
            if (run is null) return Results.NotFound();

            var dto = ToDto(runId, run);
            return Results.Ok(dto);
        })
        .WithName("GetReconciliationRun")
        .WithTags("Reconciliation Runs")
        .Produces<ReconciliationRunDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/{caseId}/accept", async (string runId, string caseId, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var seeded = await repo.SeedRunIfMissingAsync(runId, ct);
            if (!seeded) return Results.NotFound();

            var ok = await repo.AcceptCaseAsync(runId, caseId, ct);
            if (!ok) return Results.NotFound();

            return Results.Ok(new ActionResponseDto(true, "Caso aceptado correctamente", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/{caseId}/exception", async (string runId, string caseId, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var seeded = await repo.SeedRunIfMissingAsync(runId, ct);
            if (!seeded) return Results.NotFound();

            var ok = await repo.MarkExceptionAsync(runId, caseId, ct);
            if (!ok) return Results.NotFound();

            return Results.Ok(new ActionResponseDto(true, "Caso marcado como excepción", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/bulk-accept", async (string runId, BulkAcceptRequest request, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var seeded = await repo.SeedRunIfMissingAsync(runId, ct);
            if (!seeded) return Results.NotFound();

            await repo.BulkAcceptAsync(runId, request.CaseIds, ct);
            return Results.Ok(new ActionResponseDto(true, "Casos aceptados correctamente", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/reconciliation-runs/{runId}/confirm", async (string runId, IReconciliationReviewRepository repo, CancellationToken ct) =>
        {
            var seeded = await repo.SeedRunIfMissingAsync(runId, ct);
            if (!seeded) return Results.NotFound();

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

    private static ReconciliationRunDto ToDto(string runId, ReconciliationRun run)
    {
        var orderedCases = run.Cases
            .OrderBy(x => x.DebtRowNumber)
            .ThenBy(x => x.PaymentRowNumber)
            .ToList();

        var cases = orderedCases
            .Select(x => new ReconciliationCaseDto(
                x.CaseId,
                x.DebtRowNumber,
                x.PaymentRowNumber,
                x.Customer,
                x.DebtAmount,
                x.PaymentAmount,
                x.Delta,
                x.Rule,
                x.Status,
                x.Confidence,
                x.MatchType,
                x.Evidence,
                x.Suggestion))
            .ToList();

        var summary = new RunSummaryDto(
            RunId: runId,
            CompanyId: "garcia-sa",
            CompanyName: "Alimentos Garcia SA",
            Period: "2026-03",
            Status: run.Status,
            TotalCases: cases.Count,
            ResolvedCases: cases.Count(c => c.Status is "ok" or "exception"),
            AutomaticCases: cases.Count(c => c.Status == "ok"),
            PendingCases: cases.Count(c => c.Status == "pending"),
            ExceptionCases: cases.Count(c => c.Status == "exception"),
            DebtsRowsTotal: 28,
            PaymentsRowsTotal: 26,
            DebtsAmountTotal: 1250000m,
            PaymentsAmountTotal: 1240000m
        );

        return new ReconciliationRunDto(summary, cases);
    }

    public sealed record BulkAcceptRequest(List<string> CaseIds);
}
