namespace ZeroDawn.Web.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendConfirmationEmailAsync(string toEmail, string userName, string confirmationLink);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
}
