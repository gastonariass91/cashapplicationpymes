using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ReconciliationApp.API.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
           .WithName("Health")
           .WithOpenApi();

        return app;
    }
}
