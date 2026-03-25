#nullable enable

using System.ComponentModel.DataAnnotations;

namespace ZeroDawn.Shared.Contracts.Validation;

[AttributeUsage(AttributeTargets.Property)]
public class PasswordStrengthAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string password)
        {
            return ValidationResult.Success;
        }

        if (password.Length < 6)
        {
            return new ValidationResult("Password must be at least 6 characters.");
        }

        if (!password.Any(char.IsDigit))
        {
            return new ValidationResult("Password must contain at least one digit.");
        }

        if (!password.Any(char.IsUpper))
        {
            return new ValidationResult("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            return new ValidationResult("Password must contain at least one lowercase letter.");
        }

        return ValidationResult.Success;
    }
}
