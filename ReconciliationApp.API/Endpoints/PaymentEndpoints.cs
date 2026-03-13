using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ReconciliationApp.Application.Abstractions.Repositories;

namespace ReconciliationApp.API.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/companies/{companyId:guid}/payments", async (
            Guid companyId,
            IPaymentRepository repo,
            CancellationToken ct) =>
        {
            var payments = await repo.ListByCompanyAsync(companyId, ct);

            var result = payments.Select(x => new
            {
                x.Id,
                x.CompanyId,
                x.CustomerId,
                CustomerKey = x.Customer is null ? null : x.Customer.CustomerKey,
                CustomerName = x.Customer is null ? null : x.Customer.Name,
                x.PaymentNumber,
                x.PaymentDate,
                x.AccountNumber,
                x.PayerTaxId,
                x.Amount,
                x.Currency,
                x.Status,
                x.SourceBatchRunId,
                x.CreatedAt,
                x.UpdatedAt
            });

            return Results.Ok(result);
        })
        .WithName("ListPayments")
        .WithTags("Payments")
        .Produces(StatusCodes.Status200OK)
        .WithOpenApi();

        return app;
    }
}
