#nullable enable

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using ZeroDawn.Web.Configuration;

namespace ZeroDawn.Web.Services;

public class MailKitEmailService : IEmailService
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(
        IOptions<SmtpOptions> smtpOptions,
        ILogger<MailKitEmailService> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("SMTP is not fully configured. Email sending is disabled.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpOptions.FromName, _smtpOptions.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder
            {
                HtmlBody = htmlBody
            }.ToMessageBody();

            using var client = new SmtpClient();
            var secureSocketOptions = _smtpOptions.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, secureSocketOptions);

            if (!string.IsNullOrWhiteSpace(_smtpOptions.Username))
            {
                await client.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogDebug("Email sent successfully. Subject: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email. Subject: {Subject}", subject);
        }
    }

    public Task SendConfirmationEmailAsync(string toEmail, string userName, string confirmationLink)
        => SendEmailAsync(
            toEmail,
            "Confirm your email",
            BuildTemplate(
                title: "Confirm your email",
                greetingName: userName,
                bodyText: "Please confirm your email address to finish setting up your account.",
                actionText: "Confirm Email",
                actionLink: confirmationLink,
                noteText: "This confirmation link may expire. If it does, request a new confirmation email."));

    public Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
        => SendEmailAsync(
            toEmail,
            "Reset your password",
            BuildTemplate(
                title: "Reset your password",
                greetingName: userName,
                bodyText: "We received a request to reset your password. Use the button below to continue.",
                actionText: "Reset Password",
                actionLink: resetLink,
                noteText: "This reset link may expire. If you did not request this, you can safely ignore this email."));

    private bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_smtpOptions.Host)
           && !string.IsNullOrWhiteSpace(_smtpOptions.FromEmail)
           && (!RequiresAuthentication()
               || (!string.IsNullOrWhiteSpace(_smtpOptions.Username)
                   && !string.IsNullOrWhiteSpace(_smtpOptions.Password)));

    private bool RequiresAuthentication()
        => !string.IsNullOrWhiteSpace(_smtpOptions.Username)
           || !string.IsNullOrWhiteSpace(_smtpOptions.Password);

    private string BuildTemplate(
        string title,
        string greetingName,
        string bodyText,
        string actionText,
        string actionLink,
        string noteText)
    {
        var safeName = System.Net.WebUtility.HtmlEncode(greetingName);
        var safeLink = System.Net.WebUtility.HtmlEncode(actionLink);
        var appName = string.IsNullOrWhiteSpace(_smtpOptions.FromName) ? "ZeroDawn" : _smtpOptions.FromName;

        return $$"""
        <div style="margin:0;padding:24px;background:#f3f5f8;font-family:Segoe UI,Arial,sans-serif;color:#1f2937;">
            <div style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:18px;overflow:hidden;">
                <div style="padding:24px 28px;background:linear-gradient(135deg,#172033,#26344d);color:#ffffff;">
                    <div style="font-size:14px;letter-spacing:0.08em;text-transform:uppercase;opacity:0.8;">{{appName}}</div>
                    <h1 style="margin:10px 0 0;font-size:28px;line-height:1.2;">{{title}}</h1>
                </div>
                <div style="padding:28px;">
                    <p style="margin:0 0 16px;font-size:16px;">Hello {{safeName}},</p>
                    <p style="margin:0 0 24px;font-size:16px;line-height:1.7;color:#4b5563;">{{bodyText}}</p>
                    <div style="margin:0 0 24px;">
                        <a href="{{safeLink}}" style="display:inline-block;padding:14px 22px;background:#d95c37;color:#ffffff;text-decoration:none;font-weight:700;border-radius:999px;">
                            {{actionText}}
                        </a>
                    </div>
                    <p style="margin:0 0 16px;font-size:14px;line-height:1.7;color:#6b7280;">{{noteText}}</p>
                    <p style="margin:0;font-size:13px;line-height:1.7;color:#9ca3af;word-break:break-all;">
                        If the button does not work, copy and paste this link into your browser:<br />
                        <a href="{{safeLink}}" style="color:#2563eb;text-decoration:none;">{{safeLink}}</a>
                    </p>
                </div>
            </div>
        </div>
        """;
    }
}
