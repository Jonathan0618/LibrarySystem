using LibrarySystem.Domain.Notifications;
namespace LibrarySystem.Application.Notifications;
public sealed class NotificationOperationsDto
{
    public bool UsesProductionSmtp { get; init; }
    public bool SmtpConfigured { get; init; }
    public string SenderIdentity { get; init; } = "Development notification capture";
    public int PendingCount { get; init; }
    public int ProcessingCount { get; init; }
    public int FailedCount { get; init; }
    public int SentCount { get; init; }
    public IReadOnlyCollection<NotificationQueueItemDto> RecentItems { get; init; } = [];
}
public sealed class NotificationQueueItemDto
{
    public long Id { get; init; }
    public NotificationType Type { get; init; }
    public required string RecipientEmail { get; init; }
    public required string Subject { get; init; }
    public NotificationStatus Status { get; init; }
    public int AttemptCount { get; init; }
    public DateTime NextAttemptAtUtc { get; init; }
    public DateTime? SentAtUtc { get; init; }
    public string? LastError { get; init; }
}
