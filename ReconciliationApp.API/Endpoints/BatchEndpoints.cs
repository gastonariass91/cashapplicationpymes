using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ReconciliationApp.API.Contracts;
using ReconciliationApp.Application.Features.Batches.CreateBatch;
using ReconciliationApp.Application.Features.Batches.CreateRun;

namespace ReconciliationApp.API.Endpoints;

public static class BatchEndpoints
{
    public static IEndpointRouteBuilder MapBatchEndpoints(this IEndpointRouteBuilder app)
    {
        // Batches
        app.MapPost("/batches", async (CreateBatchRequest req, CreateBatchHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new CreateBatchCommand(req.CompanyId, req.PeriodFrom, req.PeriodTo), ct);
            return Results.Created($"/batches/{result.BatchId}", result);
        })
        .WithName("CreateBatch")
        .WithTags("Batches")
        .WithOpenApi();

        // Runs
        app.MapPost("/batches/{batchId:guid}/runs", async (Guid batchId, CreateRunHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new CreateRunCommand(batchId), ct);
            return Results.Ok(result);
        })
        .WithName("CreateRun")
        .WithTags("Runs")
        .WithOpenApi();

        return app;
    }
}
