using LibrarySystem.Infrastructure.Identity;
using LibrarySystem.Infrastructure.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Infrastructure.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0); builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(x => x.IsActive);
    }
}

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder) =>
        builder.Property(x => x.Description).HasMaxLength(250);
}

public sealed class LibraryMemberConfiguration : IEntityTypeConfiguration<LibraryMember>
{
    public void Configure(EntityTypeBuilder<LibraryMember> builder)
    {
        builder.ToTable("LibraryMembers"); builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.MemberNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Grade).HasMaxLength(50); builder.Property(x => x.Department).HasMaxLength(100);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0); builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(x => x.MemberNumber).IsUnique(); builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => new { x.MemberType, x.IsActive });
        builder.HasOne(x => x.User).WithOne(x => x.LibraryMember).HasForeignKey<LibraryMember>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
