using Microsoft.AspNetCore.Mvc;
using ReconciliationApp.Application.Features.Auth.Login;
using ReconciliationApp.Application.Features.Auth.Register;

namespace ReconciliationApp.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest req,
            [FromServices] LoginHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new LoginCommand(req.Email, req.Password), ct);

            if (result is null)
                return Results.Unauthorized();

            return Results.Ok(new LoginResponse(
                result.Token,
                result.Email,
                result.FullName,
                result.Role,
                result.CompanyId,
                result.ExpiresAt));
        })
        .WithName("Login")
        .WithTags("Auth")
        .AllowAnonymous()
        .WithOpenApi();

        app.MapPost("/auth/register", async (
            RegisterRequest req,
            [FromServices] RegisterUserHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new RegisterUserCommand(
                    req.CompanyId,
                    req.Email,
                    req.Password,
                    req.FullName,
                    req.Role ?? "Operator"),
                ct);

            return Results.Created($"/users/{result.UserId}", result);
        })
        .WithName("RegisterUser")
        .WithTags("Auth")
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        return app;
    }

    public sealed record LoginRequest(string Email, string Password);

    public sealed record LoginResponse(
        string Token,
        string Email,
        string FullName,
        string Role,
        Guid CompanyId,
        DateTimeOffset ExpiresAt);

    public sealed record RegisterRequest(
        Guid CompanyId,
        string Email,
        string Password,
        string FullName,
        string? Role);
}
