using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ReconciliationApp.API.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Liveness: el proceso vive
        app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
           .WithName("Liveness")
           .WithTags("Health")
           .WithOpenApi();

        // Readiness: depende de checks (DB)
        app.MapHealthChecks("/readyz", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        })
        .WithName("Readiness")
        .WithTags("Health");

        return app;
    }
}
