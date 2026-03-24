namespace ZeroDawn.Shared.Contracts.Auth;

using System.ComponentModel.DataAnnotations;

public class ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
}
