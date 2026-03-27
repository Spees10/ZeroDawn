#nullable enable

using System.ComponentModel.DataAnnotations;
using ZeroDawn.Shared.Contracts.Validation;

namespace ZeroDawn.Shared.Contracts.Users;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [MaxLength(100, ErrorMessage = ValidationMessages.NameTooLong)]
    public string FullName { get; set; } = string.Empty;
}
