using System.Data;
using LibrarySystem.Application.Fines;
using LibrarySystem.Application.Notifications;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Domain.Notifications;
using LibrarySystem.Domain.Reservations;
using LibrarySystem.Infrastructure.Circulation;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Fines;
using LibrarySystem.Infrastructure.Notifications;
using LibrarySystem.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Services.Fines;

public sealed partial class OverdueProcessor(
    ApplicationDbContext dbContext,
    INotificationSender sender,
    IOptions<NotificationDeliveryOptions> deliveryOptions,
    TimeProvider timeProvider,
    ILogger<OverdueProcessor> logger) : IOverdueProcessor
{
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var loans = await dbContext.Loans.Include(item => item.Member).ThenInclude(member => member.User)
            .Include(item => item.BookCopy).ThenInclude(copy => copy.Book)
            .Include(item => item.History)
            .Where(item => (item.Status == LoanStatus.Active || item.Status == LoanStatus.Overdue) && item.DueAtUtc < now)
            .ToListAsync(cancellationToken);
        var policies = await dbContext.LibraryPolicies.AsNoTracking().Where(item => item.IsActive).ToDictionaryAsync(item => item.MemberType, cancellationToken);
        foreach (var loan in loans)
        {
            if (!policies.TryGetValue(loan.Member.MemberType, out var policy)) continue;
            if (loan.Status == LoanStatus.Active)
            {
                loan.Status = LoanStatus.Overdue;
                loan.History.Add(new LoanHistory { Loan = loan, Action = LoanHistoryAction.MarkedOverdue, OccurredAtUtc = now, Notes = "Marked overdue automatically." });
            }
            var overdueDays = Math.Max(0, (now.Date - loan.DueAtUtc.Date).Days - policy.FineGracePeriodDays);
            var assessed = Math.Min(policy.MaximumOverdueFine, decimal.Round(overdueDays * policy.DailyOverdueFine, 2, MidpointRounding.AwayFromZero));
            if (assessed > 0) await UpsertFineAsync(loan, assessed, now, cancellationToken);
            await QueueAsync($"overdue:{loan.Id}:{now:yyyyMMdd}", NotificationType.Overdue, loan.Member.User.Email!, "Library book overdue", $"{loan.BookCopy.Book.Title} was due on {loan.DueAtUtc:d}.", cancellationToken);
        }
        var dueSoon = await dbContext.Loans.Include(item => item.Member).ThenInclude(member => member.User).Include(item => item.BookCopy).ThenInclude(copy => copy.Book)
            .Where(item => item.Status == LoanStatus.Active && item.DueAtUtc >= now && item.DueAtUtc < now.Date.AddDays(3)).ToListAsync(cancellationToken);
        foreach (var loan in dueSoon) await QueueAsync($"due:{loan.Id}:{loan.DueAtUtc:yyyyMMdd}", NotificationType.DueSoon, loan.Member.User.Email!, "Library book due soon", $"{loan.BookCopy.Book.Title} is due on {loan.DueAtUtc:d}.", cancellationToken);
        var ready = await dbContext.Reservations.Include(item => item.Member).ThenInclude(member => member.User).Include(item => item.Book)
            .Where(item => item.Status == ReservationStatus.ReadyForPickup).ToListAsync(cancellationToken);
        foreach (var reservation in ready) await QueueAsync($"ready:{reservation.Id}:{reservation.ReadyAtUtc:yyyyMMddHHmmss}", NotificationType.ReadyForPickup, reservation.Member.User.Email!, "Reservation ready for pickup", $"{reservation.Book.Title} is ready until {reservation.ExpiresAtUtc:d}.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeliverNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var options = deliveryOptions.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var claimTransaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var notifications = await dbContext.QueuedNotifications
            .Where(item =>
                (item.Status == NotificationStatus.Pending ||
                 item.Status == NotificationStatus.Failed ||
                 item.Status == NotificationStatus.Processing) &&
                item.AttemptCount < options.MaximumAttempts &&
                item.NextAttemptAtUtc <= now)
            .OrderBy(item => item.Id)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            notification.Status = NotificationStatus.Processing;
            notification.NextAttemptAtUtc = now.AddMinutes(options.LeaseMinutes);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await claimTransaction.CommitAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            try
            {
                await sender.SendAsync(notification.RecipientEmail, notification.Subject, notification.Body, cancellationToken);
                notification.Status = NotificationStatus.Sent;
                notification.SentAtUtc = timeProvider.GetUtcNow().UtcDateTime;
                notification.LastError = null;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                notification.AttemptCount++;
                notification.Status = NotificationStatus.Failed;
                notification.LastError = exception.Message.Length > 1000 ? exception.Message[..1000] : exception.Message;
                var retryDelayMinutes = Math.Min(
                    options.MaximumRetryDelayMinutes,
                    Math.Pow(2, notification.AttemptCount));
                notification.NextAttemptAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(retryDelayMinutes);
                LogDeliveryFailed(logger, exception, notification.Id);
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task UpsertFineAsync(Loan loan, decimal assessed, DateTime now, CancellationToken cancellationToken)
    {
        var fine = await dbContext.Fines.Include(item => item.Transactions).SingleOrDefaultAsync(item => item.LoanId == loan.Id && item.Type == FineType.Overdue, cancellationToken);
        if (fine is null)
        {
            fine = new Fine { Member = loan.Member, MemberId = loan.MemberId, Loan = loan, LoanId = loan.Id, Type = FineType.Overdue, AssessedAmount = assessed, Balance = assessed, Reason = $"Overdue loan #{loan.Id}." };
            fine.Transactions.Add(new FineTransaction { Fine = fine, Type = FineTransactionType.Charge, Amount = assessed, Reason = "Initial overdue assessment." });
            dbContext.Fines.Add(fine);
        }
        else if (assessed > fine.AssessedAmount && fine.Status is not FineStatus.Waived and not FineStatus.Paid)
        {
            var increase = assessed - fine.AssessedAmount; fine.AssessedAmount = assessed; fine.Balance += increase; fine.UpdatedAtUtc = now;
            fine.Transactions.Add(new FineTransaction { Fine = fine, Type = FineTransactionType.Charge, Amount = increase, Reason = "Additional overdue assessment." });
        }
    }

    private async Task QueueAsync(string key, NotificationType type, string email, string subject, string body, CancellationToken cancellationToken)
    {
        if (!dbContext.QueuedNotifications.Local.Any(item => item.DeduplicationKey == key) &&
            !await dbContext.QueuedNotifications.AnyAsync(item => item.DeduplicationKey == key, cancellationToken))
            dbContext.QueuedNotifications.Add(new QueuedNotification { DeduplicationKey = key, Type = type, RecipientEmail = email, Subject = subject, Body = body });
    }

    [LoggerMessage(2, LogLevel.Error, "Notification {NotificationId} delivery failed.")]
    private static partial void LogDeliveryFailed(ILogger logger, Exception exception, long notificationId);
}
