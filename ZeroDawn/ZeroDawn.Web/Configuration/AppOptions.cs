namespace ZeroDawn.Web.Configuration;

public class AppOptions
{
    public const string Section = "App";
    public string AppName { get; set; } = "ZeroDawn";
    public string BaseUrl { get; set; } = "";
    public bool AllowSelfRegistration { get; set; } = true;
    public bool RequireEmailConfirmation { get; set; } = true;
    public string DefaultLanguage { get; set; } = "ar";
}
