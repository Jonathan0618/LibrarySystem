using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Services.Operations;

public sealed class DataRetentionOptions
{
    public const string SectionName = "DataRetention";

    public bool Enabled { get; init; }

    [Range(365, 36500)]
    public int AuditLogDays { get; init; } = 2555;

    [Range(1, 3650)]
    public int TerminalNotificationDays { get; init; } = 90;

    [Range(1, 10000)]
    public int BatchSize { get; init; } = 500;

    [Range(1, 168)]
    public int IntervalHours { get; init; } = 24;
}
