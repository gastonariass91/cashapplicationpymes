using Microsoft.EntityFrameworkCore;
using ReconciliationApp.API.Contracts;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.DependencyInjection;
using ReconciliationApp.Application.Features.Batches.CreateBatch;
using ReconciliationApp.Application.Features.Batches.CreateRun;
using ReconciliationApp.Application.Features.Companies.CreateCompany;
using ReconciliationApp.Application.Features.Imports;
using ReconciliationApp.Application.Features.Imports.UploadDebtCsv;
using ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv;
using ReconciliationApp.Domain.Entities.Reconciliation;
using ReconciliationApp.Infrastructure.DependencyInjection;
using ReconciliationApp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// DI modular (profesional)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Handlers (por ahora directos; luego podemos pasar a MediatR)
builder.Services.AddScoped<CreateCompanyHandler>();
builder.Services.AddScoped<CreateBatchHandler>();
builder.Services.AddScoped<CreateRunHandler>();
builder.Services.AddScoped<UploadDebtCsvHandler>();
builder.Services.AddScoped<UploadPaymentsCsvHandler>();

// Observabilidad / Errores estándar
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

// Reconciliation
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

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health")
   .WithOpenApi();

// Companies
app.MapPost("/companies", async (CreateCompanyRequest req, CreateCompanyHandler handler, CancellationToken ct) =>
{
    var result = await handler.Handle(new CreateCompanyCommand(req.Name), ct);
    return Results.Created($"/companies/{result.CompanyId}", result);
})
.WithName("CreateCompany")
.WithOpenApi();

// Batches
app.MapPost("/batches", async (CreateBatchRequest req, CreateBatchHandler handler, CancellationToken ct) =>
{
    var result = await handler.Handle(new CreateBatchCommand(req.CompanyId, req.PeriodFrom, req.PeriodTo), ct);
    return Results.Created($"/batches/{result.BatchId}", result);
})
.WithName("CreateBatch")
.WithOpenApi();

// Runs
app.MapPost("/batches/{batchId:guid}/runs", async (Guid batchId, CreateRunHandler handler, CancellationToken ct) =>
{
    var result = await handler.Handle(new CreateRunCommand(batchId), ct);
    return Results.Ok(result);
})
.WithName("CreateRun")
.WithOpenApi();

// Imports
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
.WithOpenApi();

app.Run();
