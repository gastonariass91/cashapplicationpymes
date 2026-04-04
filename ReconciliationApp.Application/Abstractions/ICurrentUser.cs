namespace ReconciliationApp.Application.Abstractions;
public interface ICurrentUser
{
    Guid UserId { get; }
    Guid CompanyId { get; }
    string Email { get; }
    string Role { get; }
    string CompanyName { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin => Role == "Admin";
}
