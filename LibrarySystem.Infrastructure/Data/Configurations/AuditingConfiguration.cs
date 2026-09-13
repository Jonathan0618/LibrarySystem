using LibrarySystem.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorUserId).HasMaxLength(450);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => new { x.CreatedAtUtc, x.Id });
        builder.HasIndex(x => new { x.TargetType, x.TargetId });
    }
}
