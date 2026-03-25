namespace ZeroDawn.Shared.Contracts.Auth;

using System.ComponentModel.DataAnnotations;
using ZeroDawn.Shared.Contracts.Validation;

public class RegisterRequest
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [MaxLength(50, ErrorMessage = ValidationMessages.NameTooLong)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.EmailInvalid)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = ValidationMessages.Required)]
    [PasswordStrength]
    [MaxLength(100)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = ValidationMessages.Required)]
    [Compare(nameof(Password), ErrorMessage = ValidationMessages.PasswordsDoNotMatch)]
    public string ConfirmPassword { get; set; } = "";
}
