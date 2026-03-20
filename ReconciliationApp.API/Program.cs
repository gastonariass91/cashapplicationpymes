using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using ReconciliationApp.API.Endpoints;
using ReconciliationApp.API.ErrorHandling;
using ReconciliationApp.API.Observability;
using ReconciliationApp.API.Validation;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.DependencyInjection;
using ReconciliationApp.Infrastructure.Auth;
using ReconciliationApp.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// DI modular
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ICurrentUser — lee el CompanyId y Role del JWT en cada request
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// Validation (FluentValidation)
builder.Services.AddValidatorsFromAssemblyContaining<CreateCompanyRequestValidator>();

// Health checks
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddHealthChecks();//
    //.AddNpgsql(cs!);

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

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required in configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"] ?? "ReconciliationApp",
            ValidAudience            = builder.Configuration["Jwt:Audience"] ?? "ReconciliationApp",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Política por defecto: cualquier usuario autenticado
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Política solo para Admin
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Permite enviar el token JWT desde Swagger UI
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

// Auth middleware (orden importa: primero authn, después authz)
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapHealthEndpoints();
app.MapAuthEndpoints();           // POST /auth/login (público), POST /auth/register (Admin)
app.MapCompanyEndpoints();
app.MapBatchEndpoints();
app.MapImportEndpoints();
app.MapReconciliationEndpoints();
app.MapReconciliationRunQueryEndpoints();

app.Run();

public partial class Program { } // expone Program para WebApplicationFactory

