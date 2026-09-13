namespace LibrarySystem.Application.Notifications;

public interface IAccountNotificationService
{
    Task QueueAsync(
        string deduplicationKey,
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
