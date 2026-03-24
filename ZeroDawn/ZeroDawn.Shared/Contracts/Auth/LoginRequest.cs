namespace ZeroDawn.Shared.Contracts.Auth;

using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
}
