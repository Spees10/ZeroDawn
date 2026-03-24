namespace ZeroDawn.Shared.Core;

public class ApiEndpoints
{
    public string BaseUrl { get; set; } = "";

    public static class Auth
    {
        public const string Login = "api/auth/login";
        public const string Register = "api/auth/register";
        public const string RefreshToken = "api/auth/refresh";
        public const string ForgotPassword = "api/auth/forgot-password";
        public const string ResetPassword = "api/auth/reset-password";
        public const string ChangePassword = "api/auth/change-password";
        public const string ConfirmEmail = "api/auth/confirm-email";
        public const string ResendConfirmation = "api/auth/resend-confirmation";
        public const string Logout = "api/auth/logout";
    }

    public static class Users
    {
        public const string GetAll = "api/users";
        public const string GetById = "api/users/{0}";
        public const string Profile = "api/users/profile";
        public const string UpdateProfile = "api/users/profile";
    }

    public static class Admin
    {
        public const string ErrorLogs = "api/admin/error-logs";
        public const string ManageUsers = "api/admin/users";
        public const string ManageAdmins = "api/admin/admins";
    }
}
