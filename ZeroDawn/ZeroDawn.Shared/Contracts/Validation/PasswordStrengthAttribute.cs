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
            return new ValidationResult("كلمة المرور يجب ألا تقل عن 6 أحرف.");
        }

        if (!password.Any(char.IsDigit))
        {
            return new ValidationResult("كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.");
        }

        if (!password.Any(char.IsUpper))
        {
            return new ValidationResult("كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل.");
        }

        if (!password.Any(char.IsLower))
        {
            return new ValidationResult("كلمة المرور يجب أن تحتوي على حرف صغير واحد على الأقل.");
        }

        return ValidationResult.Success;
    }
}
