using Microsoft.AspNetCore.Mvc;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;

namespace ReconciliationApp.API.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/companies/{companyId:guid}/customers", async (
            Guid companyId,
            [FromServices] ICustomerRepository repo,
            [FromServices] ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            // Solo puede ver clientes de su propia empresa
            if (currentUser.CompanyId != companyId)
                return Results.Forbid();

            var customers = await repo.ListByCompanyAsync(companyId, ct);

            var dtos = customers.Select(c => new CustomerDto(
                c.Id,
                c.CustomerKey,
                c.Name,
                c.Email,
                c.CreatedAt,
                c.UpdatedAt));

            return Results.Ok(dtos);
        })
        .WithName("ListCustomers")
        .WithTags("Customers")
        .RequireAuthorization()
        .WithOpenApi();

        return app;
    }

    public sealed record CustomerDto(
        Guid Id,
        string CustomerKey,
        string Name,
        string? Email,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
