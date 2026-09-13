using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Notifications;

[Table("QueuedNotifications")]
[Index(nameof(DeduplicationKey), IsUnique = true)]
[Index(nameof(Status), nameof(NextAttemptAtUtc))]
public sealed class QueuedNotification
{
    [Key] public long Id { get; set; }
    [Required, MaxLength(200)] public required string DeduplicationKey { get; set; }
    [EnumDataType(typeof(NotificationType))] public NotificationType Type { get; set; }
    [Required, EmailAddress, MaxLength(256)] public required string RecipientEmail { get; set; }
    [Required, MaxLength(300)] public required string Subject { get; set; }
    [Required, MaxLength(4000)] public required string Body { get; set; }
    [EnumDataType(typeof(NotificationStatus))] public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    [Range(0, 20)] public int AttemptCount { get; set; }
    [Precision(0)] public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [Precision(0)] public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;
    [Precision(0)] public DateTime? SentAtUtc { get; set; }
    [MaxLength(1000)] public string? LastError { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
