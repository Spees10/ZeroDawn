namespace ZeroDawn.Shared.Contracts.Auth;

using System.ComponentModel.DataAnnotations;
using ZeroDawn.Shared.Contracts.Validation;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = ValidationMessages.Required)]
    [PasswordStrength]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = ValidationMessages.Required)]
    [Compare(nameof(NewPassword), ErrorMessage = ValidationMessages.PasswordsDoNotMatch)]
    public string ConfirmPassword { get; set; } = "";
}
