using LibrarySystem.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Infrastructure.Data.Configurations;

public sealed class QueuedNotificationConfiguration : IEntityTypeConfiguration<QueuedNotification>
{
    public void Configure(EntityTypeBuilder<QueuedNotification> builder)
    {
        builder.ToTable("QueuedNotifications"); builder.HasKey(x => x.Id);
        builder.Property(x => x.DeduplicationKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0); builder.Property(x => x.NextAttemptAtUtc).HasPrecision(0);
        builder.Property(x => x.SentAtUtc).HasPrecision(0); builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.DeduplicationKey).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
    }
}
