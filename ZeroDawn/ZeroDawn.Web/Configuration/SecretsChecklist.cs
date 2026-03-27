#nullable enable

namespace ZeroDawn.Web.Configuration;

/// <summary>
/// Central reference for all User Secrets required by this project.
/// Run these commands after cloning to configure secrets:
///
/// cd ZeroDawn\ZeroDawn\ZeroDawn.Web
/// dotnet user-secrets init  (only needed once)
/// dotnet user-secrets set "Jwt:Secret" "your-64-char-min-secret-key-here-change-this-to-something-unique!!"
/// dotnet user-secrets set "Smtp:Username" "your-smtp-username"
/// dotnet user-secrets set "Smtp:Password" "your-smtp-password"
///
/// Optional overrides (have defaults in appsettings.json):
/// dotnet user-secrets set "Database:ConnectionString" "Server=...;Database=...;Trusted_Connection=True;"
/// dotnet user-secrets set "App:BaseUrl" "https://localhost:7001"
/// </summary>
public static class SecretsChecklist
{
    /// <summary>
    /// All required secret keys that MUST be set via User Secrets before running.
    /// </summary>
    public static readonly IReadOnlyList<SecretEntry> Required =
    [
        new("Jwt:Secret", "JWT signing key (minimum 64 characters)", IsSet: false),
        new("Smtp:Username", "SMTP server username for sending emails", IsSet: false),
        new("Smtp:Password", "SMTP server password", IsSet: false),
    ];

    /// <summary>
    /// Optional secret keys that have defaults but can be overridden.
    /// </summary>
    public static readonly IReadOnlyList<SecretEntry> Optional =
    [
        new("Database:ConnectionString", "SQL Server connection string (default: localdb)", IsSet: false),
        new("App:BaseUrl", "Application base URL (default: https://localhost:7001)", IsSet: false),
    ];

    /// <summary>
    /// Validates that all required secrets are configured.
    /// Call this during startup to get early warnings.
    /// </summary>
    public static List<string> ValidateSecrets(IConfiguration configuration)
    {
        var missing = new List<string>();

        foreach (var secret in Required)
        {
            var value = configuration[secret.Key];
            if (string.IsNullOrWhiteSpace(value))
            {
                missing.Add($"  dotnet user-secrets set \"{secret.Key}\" \"<{secret.Description}>\"");
            }
        }

        return missing;
    }

    public record SecretEntry(string Key, string Description, bool IsSet);
}
