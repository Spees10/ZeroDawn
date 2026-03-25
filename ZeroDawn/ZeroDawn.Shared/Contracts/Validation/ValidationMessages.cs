#nullable enable

namespace ZeroDawn.Shared.Contracts.Validation;

public static class ValidationMessages
{
    public const string Required = "{0} is required.";
    public const string EmailInvalid = "Invalid email address.";
    public const string PasswordTooShort = "Password must be at least {1} characters.";
    public const string PasswordsDoNotMatch = "Passwords do not match.";
    public const string NameTooLong = "Name cannot exceed {1} characters.";
}
