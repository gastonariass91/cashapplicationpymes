using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ReconciliationApp.Application.Features.Imports;
using ReconciliationApp.Application.Features.Imports.UploadDebtCsv;
using ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv;

namespace ReconciliationApp.API.Endpoints;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/batches/{batchId:guid}/runs/{runNumber:int}/debt-csv", async (
            Guid batchId,
            int runNumber,
            UploadCsvRequest req,
            UploadDebtCsvHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(batchId, runNumber, req, ct);
            return Results.NoContent();
        })
        .WithName("UploadDebtCsv")
        .WithTags("Imports")
        .WithOpenApi();

        app.MapPost("/batches/{batchId:guid}/runs/{runNumber:int}/payments-csv", async (
            Guid batchId,
            int runNumber,
            UploadCsvRequest req,
            UploadPaymentsCsvHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(batchId, runNumber, req, ct);
            return Results.NoContent();
        })
        .WithName("UploadPaymentsCsv")
        .WithTags("Imports")
        .WithOpenApi();

        return app;
    }
}
