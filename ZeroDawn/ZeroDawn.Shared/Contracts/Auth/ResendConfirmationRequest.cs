namespace ZeroDawn.Shared.Contracts.Auth;

using System.ComponentModel.DataAnnotations;

public class ResendConfirmationRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
}
