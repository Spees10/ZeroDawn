namespace ZeroDawn.Shared.Contracts.Auth;

using System.ComponentModel.DataAnnotations;

public class ResetPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Token { get; set; } = "";
    [Required, MinLength(6)] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
}
