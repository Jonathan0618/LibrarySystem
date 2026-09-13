using System.Net;
using System.Net.Mail;
using LibrarySystem.Application.Notifications;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Services.Notifications;

public sealed class SmtpNotificationSender(IOptions<SmtpOptions> options) : INotificationSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.UserName, _options.Password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
