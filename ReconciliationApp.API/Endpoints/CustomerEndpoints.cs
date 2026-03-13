using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ReconciliationApp.Application.Abstractions.Repositories;

namespace ReconciliationApp.API.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/companies/{companyId:guid}/customers", async (
            Guid companyId,
            ICustomerRepository repo,
            CancellationToken ct) =>
        {
            var customers = await repo.ListByCompanyAsync(companyId, ct);

            var result = customers.Select(x => new
            {
                x.Id,
                x.CompanyId,
                CustomerKey = x.CustomerKey,
                x.Name,
                x.Email,
                x.CreatedAt,
                x.UpdatedAt
            });

            return Results.Ok(result);
        })
        .WithName("ListCustomers")
        .WithTags("Customers")
        .Produces(StatusCodes.Status200OK)
        .WithOpenApi();

        return app;
    }
}
