using LibrarySystem.Application.Notifications;
using LibrarySystem.Domain.Notifications;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Notifications;

public sealed class AccountNotificationService(ApplicationDbContext dbContext) : IAccountNotificationService
{
    public async Task QueueAsync(
        string deduplicationKey,
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (dbContext.QueuedNotifications.Local.Any(item => item.DeduplicationKey == deduplicationKey) ||
            await dbContext.QueuedNotifications.AnyAsync(
                item => item.DeduplicationKey == deduplicationKey,
                cancellationToken))
        {
            return;
        }

        dbContext.QueuedNotifications.Add(new QueuedNotification
        {
            DeduplicationKey = deduplicationKey,
            Type = NotificationType.Account,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body
        });
    }
}
