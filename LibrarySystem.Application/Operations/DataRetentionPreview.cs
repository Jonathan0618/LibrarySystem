namespace LibrarySystem.Application.Operations;

public sealed record DataRetentionPreview(
    DateTime AuditCutoffUtc,
    DateTime NotificationCutoffUtc,
    long ExpiredAuditLogCount,
    long ExpiredTerminalNotificationCount);
