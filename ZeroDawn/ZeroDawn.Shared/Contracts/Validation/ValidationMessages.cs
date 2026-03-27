#nullable enable

namespace ZeroDawn.Shared.Contracts.Validation;

public static class ValidationMessages
{
    public const string Required = "حقل {0} مطلوب.";
    public const string EmailInvalid = "البريد الإلكتروني غير صالح.";
    public const string PasswordTooShort = "كلمة المرور يجب ألا تقل عن {1} أحرف.";
    public const string PasswordsDoNotMatch = "كلمتا المرور غير متطابقتين.";
    public const string NameTooLong = "الاسم يجب ألا يزيد عن {1} حرفًا.";
}
