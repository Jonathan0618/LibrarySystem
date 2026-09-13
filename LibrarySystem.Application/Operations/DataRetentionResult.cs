namespace LibrarySystem.Application.Operations;

public sealed record DataRetentionResult(
    int DeletedAuditLogCount,
    int DeletedTerminalNotificationCount,
    bool MoreRecordsRemain);
