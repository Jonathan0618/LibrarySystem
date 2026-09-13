namespace LibrarySystem.Application.Notifications;

public interface INotificationSender
{
    Task SendAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken = default);
}
