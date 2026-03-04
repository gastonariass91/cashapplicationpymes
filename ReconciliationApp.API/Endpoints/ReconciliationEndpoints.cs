using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Reconciliation.ReconcileResult;
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
            IImportRowRepository importRows,
            CancellationToken ct) =>
        {
            var res = await ReconciliationApp.Application.Features.Reconciliation.Preview.ReconcilePreview
                .ExecuteAsync(batchId, runNumber, batches, batchRuns, importRows, ct);

            return Results.Ok(res);
        })
        .WithName("ReconcilePreview")
        .WithTags("Reconciliation")
        .WithOpenApi();

        app.MapPost("/batches/{batchId:guid}/runs/{runNumber:int}/reconcile", async (
            Guid batchId,
            int runNumber,
            ReconcileRunHandler handler,
            CancellationToken ct) =>
        {
            var res = await handler.Handle(batchId, runNumber, ct);
            return res is null
                ? Results.NotFound(new { message = "Run not found." })
                : Results.Ok(res);
        })
        .WithName("ReconcileRun")
        .WithTags("Reconciliation")
        .WithOpenApi();

        app.MapGet("/batches/{batchId:guid}/runs/{runNumber:int}/reconcile-result", async (
            Guid batchId,
            int runNumber,
            ReconcileResultHandler handler,
            CancellationToken ct) =>
        {
            var res = await handler.Handle(batchId, runNumber, ct);
            return res is null
                ? Results.NotFound(new { message = "Run not found." })
                : Results.Ok(res);
        })
        .WithName("ReconcileResult")
        .WithTags("Reconciliation")
        .WithOpenApi();

        return app;
    }
}
