using LibrarySystem.Application.Operations;
using LibrarySystem.Domain.Notifications;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Services.Operations;

public sealed class DataRetentionService(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<DataRetentionOptions> options) : IDataRetentionService
{
    private readonly DataRetentionOptions _options = options.Value;

    public async Task<DataRetentionPreview> PreviewAsync(CancellationToken cancellationToken = default)
    {
        var (auditCutoffUtc, notificationCutoffUtc) = GetCutoffs();
        var auditCount = await dbContext.AuditLogs.LongCountAsync(
            x => x.CreatedAtUtc < auditCutoffUtc,
            cancellationToken);
        var notificationCount = await dbContext.QueuedNotifications.LongCountAsync(
            x => (x.Status == NotificationStatus.Sent || x.Status == NotificationStatus.Failed) &&
                x.CreatedAtUtc < notificationCutoffUtc,
            cancellationToken);

        return new DataRetentionPreview(
            auditCutoffUtc,
            notificationCutoffUtc,
            auditCount,
            notificationCount);
    }

    public async Task<DataRetentionResult> ApplyAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Data retention execution is disabled.");
        }

        var (auditCutoffUtc, notificationCutoffUtc) = GetCutoffs();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var auditIds = await dbContext.AuditLogs
            .Where(x => x.CreatedAtUtc < auditCutoffUtc)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);
        var notificationIds = await dbContext.QueuedNotifications
            .Where(x => (x.Status == NotificationStatus.Sent || x.Status == NotificationStatus.Failed) &&
                x.CreatedAtUtc < notificationCutoffUtc)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        var deletedAuditLogs = auditIds.Count == 0
            ? 0
            : await dbContext.AuditLogs.Where(x => auditIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        var deletedNotifications = notificationIds.Count == 0
            ? 0
            : await dbContext.QueuedNotifications.Where(x => notificationIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);

        dbContext.AuditLogs.Add(new AuditLog
        {
            Action = "DataRetentionApplied",
            TargetType = "DataRetention",
            TargetId = timeProvider.GetUtcNow().ToString("O"),
            Details = $"Deleted audit logs: {deletedAuditLogs}; deleted terminal notifications: {deletedNotifications}.",
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DataRetentionResult(
            deletedAuditLogs,
            deletedNotifications,
            auditIds.Count == _options.BatchSize || notificationIds.Count == _options.BatchSize);
    }

    private (DateTime AuditCutoffUtc, DateTime NotificationCutoffUtc) GetCutoffs()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return (now.AddDays(-_options.AuditLogDays), now.AddDays(-_options.TerminalNotificationDays));
    }
}
