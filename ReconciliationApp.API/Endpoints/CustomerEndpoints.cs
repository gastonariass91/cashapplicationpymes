using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Infrastructure.Persistence;

namespace ReconciliationApp.API.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/companies/{companyId:guid}/customers", async (
            Guid companyId,
            [FromServices] AppDbContext db,
            [FromServices] ICurrentUser currentUser,
            CancellationToken ct) =>
        {
            if (currentUser.CompanyId != companyId)
                return Results.Forbid();

            // Deuda abierta por cliente
            var debtTotals = await db.Debts
                .AsNoTracking()
                .Where(d => d.CompanyId == companyId && d.Status == "Open")
                .GroupBy(d => d.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    TotalOutstanding = g.Sum(d => d.OutstandingAmount),
                    InvoiceCount = g.Count()
                })
                .ToListAsync(ct);

            // Pagos sin aplicar por cliente (pagos que no tienen reconciliation case aceptado)
            var appliedPaymentIds = await db.ReconciliationCases
                .AsNoTracking()
                .Where(c => c.Status == "ok" || c.Status == "accepted")
                .Select(c => c.CaseId)
                .ToListAsync(ct);

            var unapliedPayments = await db.Payments
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId && p.CustomerId != null)
                .GroupBy(p => p.CustomerId!.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    UnapliedCount = g.Count(),
                    UnapliedAmount = g.Sum(p => p.Amount)
                })
                .ToListAsync(ct);

            var debtMap    = debtTotals.ToDictionary(x => x.CustomerId);
            var paymentMap = unapliedPayments.ToDictionary(x => x.CustomerId);

            var customers = await db.Customers
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            var dtos = customers.Select(c =>
            {
                var debt    = debtMap.GetValueOrDefault(c.Id);
                var payment = paymentMap.GetValueOrDefault(c.Id);

                var totalOutstanding = debt?.TotalOutstanding ?? 0m;
                var invoiceCount     = debt?.InvoiceCount ?? 0;
                var unapliedAmount   = payment?.UnapliedAmount ?? 0m;
                var unapliedCount    = payment?.UnapliedCount ?? 0;

                var status = (totalOutstanding, unapliedAmount) switch
                {
                    (0, 0) => "Sin actividad",
                    (0, _) => "Al día",
                    (_, _) => "Con deuda"
                };

                return new CustomerDto(
                    c.Id,
                    c.CustomerKey,
                    c.Name,
                    c.Email,
                    totalOutstanding,
                    invoiceCount,
                    unapliedAmount,
                    unapliedCount,
                    status,
                    c.CreatedAt,
                    c.UpdatedAt);
            });

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
        decimal TotalOutstanding,
        int InvoiceCount,
        decimal UnapliedAmount,
        int UnapliedCount,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
