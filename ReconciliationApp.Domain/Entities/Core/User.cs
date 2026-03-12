namespace ReconciliationApp.Domain.Entities.Core;

public sealed class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = default!;

    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = "Operator";

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; private set; }

    private User() { } // EF

    public User(
        Guid companyId,
        string email,
        string fullName,
        string passwordHash,
        string role = "Operator")
    {
        CompanyId = companyId;
        Email = NormalizeRequired(email, nameof(email));
        FullName = NormalizeRequired(fullName, nameof(fullName));
        PasswordHash = NormalizeRequired(passwordHash, nameof(passwordHash));
        Role = NormalizeRole(role);
    }

    public void UpdateProfile(string fullName, string? role = null)
    {
        FullName = NormalizeRequired(fullName, nameof(fullName));

        if (!string.IsNullOrWhiteSpace(role))
            Role = NormalizeRole(role);
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = NormalizeRequired(passwordHash, nameof(passwordHash));
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void RegisterLogin(DateTimeOffset? at = null)
    {
        LastLoginAt = at ?? DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required", paramName);

        return value.Trim();
    }

    private static string NormalizeRole(string role)
    {
        var normalized = NormalizeRequired(role, nameof(role));

        return normalized switch
        {
            "Admin" => "Admin",
            "Operator" => "Operator",
            "Viewer" => "Viewer",
            _ => throw new ArgumentException("Invalid role", nameof(role))
        };
    }
}
