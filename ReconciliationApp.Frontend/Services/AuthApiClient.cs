using System.Net.Http.Json;

namespace ReconciliationApp.Frontend.Services;

public sealed record LoginRequestDto(string Email, string Password);

public sealed record LoginResponseDto(
    string Token,
    string Email,
    string FullName,
    string Role,
    Guid CompanyId,
    string CompanyName,
    DateTimeOffset ExpiresAt);

public sealed class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http) => _http = http;

    /// <summary>
    /// Intenta login. Devuelve el resultado o null si las credenciales son inválidas.
    /// Lanza excepción si hay error de red u otro error HTTP inesperado.
    /// </summary>
    public async Task<LoginResponseDto?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "/auth/login",
            new LoginRequestDto(email, password),
            ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: ct);
    }
}
