namespace ZeroDawn.Shared.Contracts.Auth;

public class AuthResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public string Email { get; init; } = "";
    public string FullName { get; init; } = "";
    public List<string> Roles { get; init; } = [];
}
