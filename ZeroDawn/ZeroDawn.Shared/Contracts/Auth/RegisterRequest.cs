namespace ZeroDawn.Shared.Contracts.Auth;

using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required, MaxLength(50)] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(6), MaxLength(100)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
}
