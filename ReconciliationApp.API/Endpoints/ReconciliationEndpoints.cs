using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Reconciliation.Preview;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileRun;

namespace ReconciliationApp.API.Endpoints;

public static class ReconciliationEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/batches/{batchId:guid}/runs/{runNumber:int}/preview", async (
            Guid batchId,
            int runNumber,
            IBatchRepository batches,
            IBatchRunRepository batchRuns,
            IDebtRepository debts,
            IPaymentRepository payments,
            CancellationToken ct) =>
        {
            var result = await ReconcilePreview.ExecuteAsync(
                batchId,
                runNumber,
                batches,
                batchRuns,
                debts,
                payments,
                ct);

            return Results.Ok(result);
        })
        .WithName("PreviewReconciliation")
        .WithTags("Reconciliation");

        app.MapPost("/batches/{batchId:guid}/runs/{runNumber:int}/reconcile", async (
            Guid batchId,
            int runNumber,
            ReconcileRunHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(batchId, runNumber, ct);

            if (result is null)
                return Results.NotFound();

            return Results.Ok(result);
        })
        .WithName("RunReconciliation")
        .WithTags("Reconciliation");

        return app;
    }
}