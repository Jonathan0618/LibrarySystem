using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Services.Notifications;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "Notifications:Delivery";

    [Range(1, 20)]
    public int MaximumAttempts { get; init; } = 5;

    [Range(1, 500)]
    public int BatchSize { get; init; } = 50;

    [Range(1, 60)]
    public int LeaseMinutes { get; init; } = 5;

    [Range(1, 1440)]
    public int MaximumRetryDelayMinutes { get; init; } = 60;
}
