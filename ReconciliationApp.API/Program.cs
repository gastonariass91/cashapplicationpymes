using ReconciliationApp.Domain.Entities.Reconciliation;

using Microsoft.EntityFrameworkCore;
using ReconciliationApp.API.Contracts;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Application.Features.Batches.CreateBatch;
using ReconciliationApp.Application.Features.Batches.CreateRun;
using ReconciliationApp.Application.Features.Companies.CreateCompany;
using ReconciliationApp.Infrastructure.Persistence;
using ReconciliationApp.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Missing ConnectionStrings:Default in configuration.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Repos (Infrastructure)
builder.Services.AddScoped<ICompanyRepository, EfCompanyRepository>();
builder.Services.AddScoped<IBatchRepository, EfBatchRepository>();
builder.Services.AddScoped<ReconciliationApp.Application.Abstractions.Sql.IRunNumberService, ReconciliationApp.Infrastructure.Sql.RunNumberService>();
builder.Services.AddScoped<ReconciliationApp.Application.Abstractions.Repositories.IBatchRunRepository, ReconciliationApp.Infrastructure.Repositories.EfBatchRunRepository>();
builder.Services.AddScoped<ReconciliationApp.Infrastructure.Sql.RunNumberService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// Handlers (CQRS)
builder.Services.AddScoped<CreateCompanyHandler>();
builder.Services.AddScoped<CreateBatchHandler>();
builder.Services.AddScoped<CreateRunHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ReconciliationApp.Application.Abstractions.Repositories.IImportRowRepository, ReconciliationApp.Infrastructure.Repositories.EfImportRowRepository>();
builder.Services.AddScoped<ReconciliationApp.Application.Features.Imports.UploadDebtCsv.UploadDebtCsvHandler>();
builder.Services.AddScoped<ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv.UploadPaymentsCsvHandler>();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/batches/{batchId:guid}/runs/{runNumber:int}/preview", async (Guid batchId, int runNumber, ReconciliationApp.Application.Abstractions.Repositories.IBatchRepository batches, ReconciliationApp.Application.Abstractions.Repositories.IBatchRunRepository batchRuns, ReconciliationApp.Application.Abstractions.Repositories.IImportRowRepository importRows, CancellationToken ct) =>
{
    var res = await ReconciliationApp.Application.Features.Reconciliation.Preview.ReconcilePreview.ExecuteAsync(batchId, runNumber, batches, batchRuns, importRows, ct);
    return Results.Ok(res);
})
.WithName("ReconcilePreview")
.WithTags("Reconciliation")
.WithOpenApi();

app.MapPost("/batches/{batchId:guid}/runs/{runNumber:int}/reconcile", async (
    Guid batchId,
    int runNumber,
    AppDbContext db,
    ReconciliationApp.Application.Abstractions.Repositories.IBatchRepository batches,
    ReconciliationApp.Application.Abstractions.Repositories.IBatchRunRepository batchRuns,
    ReconciliationApp.Application.Abstractions.Repositories.IImportRowRepository importRows,
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

    var preview = await ReconciliationApp.Application.Features.Reconciliation.Preview.ReconcilePreview.ExecuteAsync(batchId, runNumber, batches, batchRuns, importRows, ct);

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

app.MapGet("/health", () => "API Running");

// Companies
app.MapPost("/companies", async (CreateCompanyRequest req, CreateCompanyHandler handler, CancellationToken ct) =>
{
    var result = await handler.Handle(new CreateCompanyCommand(req.Name), ct);
    return Results.Created($"/companies/{result.CompanyId}", result);
});

// Batches
app.MapPost("/batches", async (CreateBatchRequest req, CreateBatchHandler handler, CancellationToken ct) =>
{
    var result = await handler.Handle(new CreateBatchCommand(req.CompanyId, req.PeriodFrom, req.PeriodTo), ct);
    return Results.Created($"/batches/{result.BatchId}", result);
});

// Runs
app.MapPost("/batches/{batchId:guid}/runs", async (Guid batchId, CreateRunHandler handler, CancellationToken ct) =>
{
    var result = await handler.Handle(new CreateRunCommand(batchId), ct);
    return Results.Ok(result);
});

app.MapPost("/batches/{batchId:guid}/runs/{runNumber:int}/debt-csv", async (Guid batchId, int runNumber, ReconciliationApp.Application.Features.Imports.UploadCsvRequest req, ReconciliationApp.Application.Features.Imports.UploadDebtCsv.UploadDebtCsvHandler handler, CancellationToken ct) =>
{
    await handler.Handle(batchId, runNumber, req, ct);
    return Results.NoContent();
})
.WithName("UploadDebtCsv")
.WithOpenApi();

app.MapPost("/batches/{batchId:guid}/runs/{runNumber:int}/payments-csv", async (Guid batchId, int runNumber, ReconciliationApp.Application.Features.Imports.UploadCsvRequest req, ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv.UploadPaymentsCsvHandler handler, CancellationToken ct) =>
{
    await handler.Handle(batchId, runNumber, req, ct);
    return Results.NoContent();
})
.WithName("UploadPaymentsCsv")
.WithOpenApi();
app.Run();
