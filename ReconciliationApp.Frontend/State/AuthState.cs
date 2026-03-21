namespace ReconciliationApp.Frontend.State;

/// <summary>
/// Estado de autenticación del usuario. Scoped — una instancia por sesión Blazor.
/// Almacena el token JWT y los datos del usuario autenticado.
/// </summary>
public sealed class AuthState
{
    public event Action? OnChange;

    public string? Token         { get; private set; }
    public string? Email         { get; private set; }
    public string? FullName      { get; private set; }
    public string? Role          { get; private set; }
    public Guid    CompanyId     { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public bool IsAuthenticated => Token is not null && ExpiresAt > DateTimeOffset.UtcNow;
    public bool IsAdmin         => Role == "Admin";

    public void SetUser(
        string token, string email, string fullName,
        string role, Guid companyId, DateTimeOffset expiresAt)
    {
        Token      = token;
        Email      = email;
        FullName   = fullName;
        Role       = role;
        CompanyId  = companyId;
        ExpiresAt  = expiresAt;
        OnChange?.Invoke();
    }

    public void ClearUser()
    {
        Token     = null;
        Email     = null;
        FullName  = null;
        Role      = null;
        CompanyId = Guid.Empty;
        ExpiresAt = default;
        OnChange?.Invoke();
    }
}
