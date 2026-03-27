#nullable enable

using System.ComponentModel.DataAnnotations;
using ZeroDawn.Shared.Contracts.Validation;

namespace ZeroDawn.Shared.Contracts.Users;

public class UpdateUserStatusRequest
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    public string UserId { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
