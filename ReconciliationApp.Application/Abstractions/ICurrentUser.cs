namespace ReconciliationApp.Application.Abstractions;

/// <summary>
/// Provee acceso al usuario autenticado en el contexto de la request actual.
/// Se inyecta en handlers que necesitan saber quién ejecuta la acción.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    Guid CompanyId { get; }
    string Email { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin => Role == "Admin";
}
