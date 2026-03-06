using Microsoft.AspNetCore.Http;
using ReconciliationApp.API.Contracts.Reconciliation;

namespace ReconciliationApp.API.Endpoints;

public static class ReconciliationRunQueryEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationRunQueryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reconciliation-runs/{runId}", (string runId) =>
        {
            var cases = new List<ReconciliationCaseDto>
            {
                new("case-1-1", 1, 1, "C1", 1000m, 1000m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto · ref coincide", "Aceptar"),
                new("case-4-3", 4, 3, "C3", 1200m, 1200m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto", "Aceptar"),
                new("case-5-4", 5, 4, "C4", 700m, 700m, 0m, "Cliente+Monto", "ok", "high", "exact", "Mismo cliente · monto exacto", "Aceptar"),
                new("case-7-6", 7, 6, "C5", 350m, 350m, 0m, "Cliente+Monto", "ok", "medium", "exact", "Cliente coincide · sin ref", "Aceptar (c/ cuidado)"),
                new("case-2-2", 2, 2, "C1", 500m, 450m, -50m, "Monto cercano", "pending", "medium", "partial", "Pago menor · posible parcial", "Revisar parcial"),
                new("case-6-8", 6, 8, "C2", 900m, 930m, 30m, "Monto cercano", "pending", "medium", "ambiguous", "Varias deudas posibles", "Revisar"),
                new("case-3-5", 3, 5, "C2", 200m, 200m, 0m, "Duplicado?", "exception", "low", "duplicate", "Pago similar ya conciliado", "Excepción")
            };

            var summary = new RunSummaryDto(
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

            return Results.Ok(new ReconciliationRunDto(summary, cases));
        })
        .WithName("GetReconciliationRun")
        .WithTags("Reconciliation Runs")
        .Produces<ReconciliationRunDto>(StatusCodes.Status200OK);

        return app;
    }
}
