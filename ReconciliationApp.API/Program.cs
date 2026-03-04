using ReconciliationApp.API.Endpoints;
using ReconciliationApp.Application.DependencyInjection;
using ReconciliationApp.Infrastructure.DependencyInjection;
using ReconciliationApp.Application.Features.Batches.CreateBatch;
using ReconciliationApp.Application.Features.Batches.CreateRun;
using ReconciliationApp.Application.Features.Companies.CreateCompany;
using ReconciliationApp.Application.Features.Imports.UploadDebtCsv;
using ReconciliationApp.Application.Features.Imports.UploadPaymentsCsv;

var builder = WebApplication.CreateBuilder(args);

// DI modular
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Handlers (por ahora directos; luego migramos a MediatR)
builder.Services.AddScoped<CreateCompanyHandler>();
builder.Services.AddScoped<CreateBatchHandler>();
builder.Services.AddScoped<CreateRunHandler>();
builder.Services.AddScoped<UploadDebtCsvHandler>();
builder.Services.AddScoped<UploadPaymentsCsvHandler>();

// Errores estándar
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

// Endpoints por feature
app.MapHealthEndpoints();
app.MapCompanyEndpoints();
app.MapBatchEndpoints();
app.MapImportEndpoints();
app.MapReconciliationEndpoints();

app.Run();
