#nullable enable

namespace ZeroDawn.Shared.Contracts.Users;

public class UserDto
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public bool EmailConfirmed { get; init; }
}
