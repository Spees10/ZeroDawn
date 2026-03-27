#nullable enable

using System.ComponentModel.DataAnnotations;
using ZeroDawn.Shared.Contracts.Validation;

namespace ZeroDawn.Shared.Contracts.Users;

public class AssignRoleRequest
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    public string Role { get; set; } = string.Empty;
}
