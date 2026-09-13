using LibrarySystem.Domain.Notifications;
namespace LibrarySystem.Application.Notifications;
public interface INotificationOperationsService
{
    Task<NotificationOperationsDto> GetAsync(CancellationToken cancellationToken = default);
    Task QueueTestAsync(string recipientEmail, NotificationType type, CancellationToken cancellationToken = default);
    Task<bool> RetryAsync(long id, CancellationToken cancellationToken = default);
}
