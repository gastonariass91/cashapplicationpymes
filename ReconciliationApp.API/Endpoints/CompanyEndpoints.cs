using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ReconciliationApp.API.Contracts;
using ReconciliationApp.Application.Features.Companies.CreateCompany;

namespace ReconciliationApp.API.Endpoints;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/companies", async (CreateCompanyRequest req, CreateCompanyHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new CreateCompanyCommand(req.Name), ct);
            return Results.Created($"/companies/{result.CompanyId}", result);
        })
        .WithName("CreateCompany")
        .WithTags("Companies")
        .WithOpenApi();

        return app;
    }
}
