using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Infrastructure.Email;

public sealed class EmailOptions
{
    public string Mode { get; set; } = "Logging";

    public string From { get; set; } = "no-reply@example.com";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public bool UseStartTls { get; set; } = true;
}

/// <summary>
/// Development and demonstration sender. It logs only routing metadata, never
/// the notification body, so ticket content does not leak into logs.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email notification accepted for delivery to {Recipient} with subject {Subject}.", Mask(message.To), message.Subject);
        return Task.CompletedTask;
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(Microsoft.Extensions.Options.IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseStartTls,
            Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.UserName, _options.Password),
        };

        using var mail = new MailMessage(_options.From, message.To, message.Subject, message.HtmlBody)
        {
            IsBodyHtml = true,
        };

        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain"));
        }

        await client.SendMailAsync(mail, cancellationToken);
    }
}
