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
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// Handlers (CQRS)
builder.Services.AddScoped<CreateCompanyHandler>();
builder.Services.AddScoped<CreateBatchHandler>();
builder.Services.AddScoped<CreateRunHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

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

app.Run();
