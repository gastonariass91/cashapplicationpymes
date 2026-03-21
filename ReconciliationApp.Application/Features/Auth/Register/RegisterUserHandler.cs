using Microsoft.Extensions.Logging;
using ReconciliationApp.Application.Abstractions;
using ReconciliationApp.Application.Abstractions.Repositories;
using ReconciliationApp.Domain.Entities.Core;

namespace ReconciliationApp.Application.Features.Auth.Register;

public sealed record RegisterUserCommand(
    Guid CompanyId,
    string Email,
    string Password,
    string FullName,
    string Role = "Operator"
);

public sealed record RegisterUserResult(Guid UserId, string Email, string Role);

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _users;
    private readonly ICompanyRepository _companies;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IUserRepository users,
        ICompanyRepository companies,
        IUnitOfWork uow,
        ILogger<RegisterUserHandler> logger)
    {
        _users = users;
        _companies = companies;
        _uow = uow;
        _logger = logger;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        var company = await _companies.GetByIdAsync(command.CompanyId, ct);
        if (company is null)
            throw new InvalidOperationException("Company not found.");

        if (await _users.ExistsByEmailAsync(email, ct))
            throw new InvalidOperationException("A user with this email already exists.");

        if (command.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);

        var user = new User(
            command.CompanyId,
            email,
            command.FullName.Trim(),
            passwordHash,
            command.Role);

        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User registered. UserId={UserId} Email={Email} CompanyId={CompanyId} Role={Role}",
            user.Id, user.Email, user.CompanyId, user.Role);

        return new RegisterUserResult(user.Id, user.Email, user.Role);
    }
}
