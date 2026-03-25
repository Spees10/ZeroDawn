namespace ZeroDawn.Shared.Contracts.Auth;

using System.ComponentModel.DataAnnotations;
using ZeroDawn.Shared.Contracts.Validation;

public class LoginRequest
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.EmailInvalid)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = ValidationMessages.Required)]
    [PasswordStrength]
    public string Password { get; set; } = "";
}
