using FluentValidation;
using Microsoft.AspNetCore.HttpLogging;
using ReconciliationApp.API.Endpoints;
using ReconciliationApp.API.ErrorHandling;
using ReconciliationApp.API.Observability;
using ReconciliationApp.API.Validation;
using ReconciliationApp.Application.DependencyInjection;
using ReconciliationApp.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// DI modular — handlers registrados dentro de AddApplication()
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Validation (FluentValidation)
builder.Services.AddValidatorsFromAssemblyContaining<CreateCompanyRequestValidator>();

// Health checks
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddHealthChecks().AddNpgSql(cs!, name: "postgres");

// Errors (ProblemDetails + ExceptionHandler)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Http Logging
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.RequestMethod
                   | HttpLoggingFields.RequestPath
                   | HttpLoggingFields.ResponseStatusCode
                   | HttpLoggingFields.Duration;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Observability
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpLogging();

// Swagger solo en Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoints por feature
app.MapHealthEndpoints();
app.MapCompanyEndpoints();
app.MapBatchEndpoints();
app.MapImportEndpoints();
app.MapReconciliationEndpoints();
app.MapReconciliationRunQueryEndpoints();

app.Run();
