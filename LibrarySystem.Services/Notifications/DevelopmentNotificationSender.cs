using LibrarySystem.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Services.Notifications;

public sealed partial class DevelopmentNotificationSender(ILogger<DevelopmentNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        LogCaptured(logger, recipientEmail, subject);
        return Task.CompletedTask;
    }

    [LoggerMessage(1, LogLevel.Information, "Captured development notification for {Recipient}: {Subject}.")]
    private static partial void LogCaptured(ILogger logger, string recipient, string subject);
}
