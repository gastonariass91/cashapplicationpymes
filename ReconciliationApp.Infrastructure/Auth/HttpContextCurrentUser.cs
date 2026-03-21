using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ReconciliationApp.Application.Abstractions;
namespace ReconciliationApp.Infrastructure.Auth;
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;
    public HttpContextCurrentUser(IHttpContextAccessor httpContext)
        => _httpContext = httpContext;
    private ClaimsPrincipal? Principal
        => _httpContext.HttpContext?.User;
    public bool IsAuthenticated
        => Principal?.Identity?.IsAuthenticated ?? false;
    public Guid UserId
        => Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id)
            ? id : Guid.Empty;
    public Guid CompanyId
        => Guid.TryParse(Principal?.FindFirst("company_id")?.Value, out var id)
            ? id : Guid.Empty;
    public string Email
        => Principal?.FindFirst("email")?.Value ?? string.Empty;
    public string Role
        => Principal?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    public string CompanyName
        => Principal?.FindFirst("company_name")?.Value ?? string.Empty;
}
