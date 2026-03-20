using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ReconciliationApp.Application.Abstractions.Repositories;

namespace ReconciliationApp.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password);

public sealed record LoginResult(
    string Token,
    string Email,
    string FullName,
    string Role,
    Guid CompanyId,
    DateTimeOffset ExpiresAt
);

public sealed class LoginHandler
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        IUserRepository users,
        IConfiguration config,
        ILogger<LoginHandler> logger)
    {
        _users = users;
        _config = config;
        _logger = logger;
    }

    public async Task<LoginResult?> Handle(LoginCommand command, CancellationToken ct)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        var user = await _users.GetByEmailAsync(email, ct);

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Login failed: user not found or inactive. Email={Email}", email);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid password. Email={Email}", email);
            return null;
        }

        var token = GenerateToken(user);

        _logger.LogInformation(
            "Login successful. UserId={UserId} Email={Email} Role={Role}",
            user.Id, user.Email, user.Role);

        return new LoginResult(
            Token: token.Token,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role,
            CompanyId: user.CompanyId,
            ExpiresAt: token.ExpiresAt);
    }

    private (string Token, DateTimeOffset ExpiresAt) GenerateToken(Domain.Entities.Core.User user)
    {
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key not configured.");

        var expiresAt = DateTimeOffset.UtcNow.AddHours(
            _config.GetValue<int>("Jwt:ExpirationHours", 8));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("company_id",                  user.CompanyId.ToString()),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim("full_name",                   user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"] ?? "ReconciliationApp",
            audience: _config["Jwt:Audience"] ?? "ReconciliationApp",
            claims:   claims,
            expires:  expiresAt.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
