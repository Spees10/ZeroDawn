namespace ZeroDawn.Web.Configuration;

public class SmtpOptions
{
    public const string Section = "Smtp";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
    public bool UseSsl { get; set; } = true;
}
