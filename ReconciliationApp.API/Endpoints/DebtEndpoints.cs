using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ReconciliationApp.Application.Abstractions.Repositories;

namespace ReconciliationApp.API.Endpoints;

public static class DebtEndpoints
{
    public static IEndpointRouteBuilder MapDebtEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/companies/{companyId:guid}/debts", async (
            Guid companyId,
            IDebtRepository repo,
            CancellationToken ct) =>
        {
            var debts = await repo.ListByCompanyAsync(companyId, ct);

            var result = debts.Select(x => new
            {
                x.Id,
                x.CompanyId,
                x.CustomerId,
                CustomerKey = x.Customer.CustomerKey,
                CustomerName = x.Customer.Name,
                x.InvoiceNumber,
                x.IssueDate,
                x.DueDate,
                x.Amount,
                x.Currency,
                x.OutstandingAmount,
                x.Status,
                x.SourceBatchRunId,
                x.CreatedAt,
                x.UpdatedAt
            });

            return Results.Ok(result);
        })
        .WithName("ListDebts")
        .WithTags("Debts")
        .Produces(StatusCodes.Status200OK)
        .WithOpenApi();

        return app;
    }
}
