using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Reconciliation;
using ReconciliationApp.Infrastructure.Persistence;

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
            AppDbContext db,
            IBatchRepository batches,
            IBatchRunRepository batchRuns,
            IImportRowRepository importRows,
            CancellationToken ct) =>
        {
            var run = await db.BatchRuns.SingleOrDefaultAsync(r => r.BatchId == batchId && r.RunNumber == runNumber, ct);
            if (run is null) return Results.NotFound(new { message = "Run not found." });

            if (run.ReconciledAt is not null)
            {
                var existing = await db.ReconciliationMatches
                    .AsNoTracking()
                    .Where(x => x.BatchRunId == run.Id)
                    .OrderBy(x => x.DebtRowNumber)
                    .Select(x => new
                    {
                        debtRowNumber = x.DebtRowNumber,
                        paymentRowNumber = x.PaymentRowNumber,
                        customerId = x.CustomerId,
                        amount = x.Amount
                    })
                    .ToListAsync(ct);

                return Results.Ok(new
                {
                    batchRunId = run.Id,
                    reconciledAt = run.ReconciledAt,
                    matchesSaved = existing.Count,
                    matches = existing
                });
            }

            var preview = await ReconciliationApp.Application.Features.Reconciliation.Preview.ReconcilePreview
                .ExecuteAsync(batchId, runNumber, batches, batchRuns, importRows, ct);

            await db.ReconciliationMatches.Where(x => x.BatchRunId == run.Id).ExecuteDeleteAsync(ct);

            var entities = preview.matches
                .Select(m => new ReconciliationMatch(run.Id, m.debtRowNumber, m.paymentRowNumber, m.customerId, m.amount))
                .ToList();

            await db.ReconciliationMatches.AddRangeAsync(entities, ct);

            run.MarkReconciled();
            await db.SaveChangesAsync(ct);

            return Results.Ok(new
            {
                batchRunId = run.Id,
                reconciledAt = run.ReconciledAt,
                matchesSaved = entities.Count,
                unmatchedDebt = preview.unmatchedDebtRowNumbers,
                unmatchedPayments = preview.unmatchedPaymentRowNumbers
            });
        })
        .WithName("ReconcileRun")
        .WithTags("Reconciliation")
        .WithOpenApi();

        app.MapGet("/batches/{batchId:guid}/runs/{runNumber:int}/reconcile-result", async (
            Guid batchId,
            int runNumber,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var run = await db.BatchRuns
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.BatchId == batchId && r.RunNumber == runNumber, ct);

            if (run is null) return Results.NotFound(new { message = "Run not found." });

            var matches = await db.ReconciliationMatches
                .AsNoTracking()
                .Where(x => x.BatchRunId == run.Id)
                .OrderBy(x => x.DebtRowNumber)
                .Select(x => new
                {
                    debtRowNumber = x.DebtRowNumber,
                    paymentRowNumber = x.PaymentRowNumber,
                    customerId = x.CustomerId,
                    amount = x.Amount
                })
                .ToListAsync(ct);

            return Results.Ok(new
            {
                batchRunId = run.Id,
                reconciledAt = run.ReconciledAt,
                matches,
                matchesSaved = matches.Count
            });
        })
        .WithName("ReconcileResult")
        .WithTags("Reconciliation")
        .WithOpenApi();

        return app;
    }
}
