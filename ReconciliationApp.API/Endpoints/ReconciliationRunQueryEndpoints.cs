using Microsoft.AspNetCore.Http;
using ReconciliationApp.API.Contracts.Reconciliation;

namespace ReconciliationApp.API.Endpoints;

public static class ReconciliationRunQueryEndpoints
{
    private static readonly Dictionary<string, List<ReconciliationCaseDto>> Runs = new();

    public static IEndpointRouteBuilder MapReconciliationRunQueryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reconciliation-runs/{runId}", (string runId) =>
        {
            var cases = GetOrSeedRun(runId);
            var summary = BuildSummary(runId, cases);
            return Results.Ok(new ReconciliationRunDto(summary, cases));
        })
        .WithName("GetReconciliationRun")
        .WithTags("Reconciliation Runs")
        .Produces<ReconciliationRunDto>(StatusCodes.Status200OK);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/{caseId}/accept", (string runId, string caseId) =>
        {
            var cases = GetOrSeedRun(runId);
            var idx = cases.FindIndex(x => x.CaseId == caseId);
            if (idx < 0) return Results.NotFound();

            var current = cases[idx];
            cases[idx] = current with
            {
                Status = "ok",
                Confidence = current.Confidence == "low" ? "medium" : current.Confidence,
                Suggestion = "Aceptar"
            };

            return Results.Ok(new ActionResponseDto(true, "Caso aceptado correctamente", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/{caseId}/exception", (string runId, string caseId) =>
        {
            var cases = GetOrSeedRun(runId);
            var idx = cases.FindIndex(x => x.CaseId == caseId);
            if (idx < 0) return Results.NotFound();

            var current = cases[idx];
            cases[idx] = current with
            {
                Status = "exception",
                Suggestion = "Excepción"
            };

            return Results.Ok(new ActionResponseDto(true, "Caso marcado como excepción", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK);

        app.MapPost("/api/reconciliation-runs/{runId}/cases/bulk-accept", (string runId, BulkAcceptRequest request) =>
        {
            var cases = GetOrSeedRun(runId);

            foreach (var caseId in request.CaseIds.Distinct())
            {
                var idx = cases.FindIndex(x => x.CaseId == caseId);
                if (idx < 0) continue;

                var current = cases[idx];
                cases[idx] = current with
                {
                    Status = "ok",
                    Confidence = current.Confidence == "low" ? "medium" : current.Confidence,
                    Suggestion = "Aceptar"
                };
            }

            return Results.Ok(new ActionResponseDto(true, "Casos aceptados correctamente", "in_review"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK);

        app.MapPost("/api/reconciliation-runs/{runId}/confirm", (string runId) =>
        {
            var cases = GetOrSeedRun(runId);
            var hasPending = cases.Any(c => c.Status == "pending");
            var hasException = cases.Any(c => c.Status == "exception");

            if (hasPending || hasException)
            {
                return Results.BadRequest(new ActionResponseDto(
                    false,
                    "No se puede confirmar: todavía hay pendientes o excepciones.",
                    "in_review"
                ));
            }

            return Results.Ok(new ActionResponseDto(true, "Conciliación confirmada", "confirmed"));
        })
        .WithTags("Reconciliation Runs")
        .Produces<ActionResponseDto>(StatusCodes.Status200OK)
        .Produces<ActionResponseDto>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static List<ReconciliationCaseDto> GetOrSeedRun(string runId)
    {
        if (Runs.TryGetValue(runId, out var existing))
            return existing;

        var seeded = new List<ReconciliationCaseDto>
        {
            new("case-1-1", 1, 1, "C1", 1000m, 1000m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto · ref coincide", "Aceptar"),
            new("case-4-3", 4, 3, "C3", 1200m, 1200m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto", "Aceptar"),
            new("case-5-4", 5, 4, "C4", 700m, 700m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto", "Aceptar"),
            new("case-7-6", 7, 6, "C5", 350m, 350m, 0m, "Cliente+Monto", "ok", "medium", "exact", "Cliente coincide · sin ref", "Aceptar (c/ cuidado)"),
            new("case-2-2", 2, 2, "C1", 500m, 450m, -50m, "Monto cercano", "pending", "medium", "partial", "Pago menor · posible parcial", "Revisar parcial"),
            new("case-6-8", 6, 8, "C2", 900m, 930m, 30m, "Monto cercano", "pending", "medium", "ambiguous", "Varias deudas posibles", "Revisar"),
            new("case-3-5", 3, 5, "C2", 200m, 200m, 0m, "Duplicado?", "exception", "low", "duplicate", "Pago similar ya conciliado", "Excepción")
        };

        Runs[runId] = seeded;
        return seeded;
    }

    private static RunSummaryDto BuildSummary(string runId, List<ReconciliationCaseDto> cases)
    {
        return new RunSummaryDto(
            RunId: runId,
            CompanyId: "garcia-sa",
            CompanyName: "Alimentos Garcia SA",
            Period: "2026-03",
            Status: "in_review",
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
    }

    public sealed record BulkAcceptRequest(List<string> CaseIds);
}
