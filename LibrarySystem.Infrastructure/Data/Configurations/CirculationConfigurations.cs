using LibrarySystem.Infrastructure.Circulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Infrastructure.Data.Configurations;

public sealed class LibraryPolicyConfiguration : IEntityTypeConfiguration<LibraryPolicy>
{
    public void Configure(EntityTypeBuilder<LibraryPolicy> builder)
    {
        builder.ToTable("CirculationPolicies"); builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.MemberType).IsUnique();
        builder.Property(x => x.DailyOverdueFine).HasPrecision(10, 2);
        builder.Property(x => x.MaximumOverdueFine).HasPrecision(10, 2);
        builder.Property(x => x.LostItemFine).HasPrecision(10, 2);
        builder.Property(x => x.DamagedItemFine).HasPrecision(10, 2);
        builder.Property(x => x.MaximumOutstandingBalanceForCheckout).HasPrecision(10, 2);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0); builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("CirculationLoans"); builder.HasKey(x => x.Id);
        builder.Property(x => x.CheckedOutAtUtc).HasPrecision(0); builder.Property(x => x.DueAtUtc).HasPrecision(0);
        builder.Property(x => x.ReturnedAtUtc).HasPrecision(0); builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.MemberId, x.Status }); builder.HasIndex(x => new { x.BookCopyId, x.Status });
        builder.HasIndex(x => new { x.CheckoutOperationId, x.BookCopyId }).IsUnique();
        builder.HasIndex(x => new { x.DueAtUtc, x.Status }); builder.HasIndex(x => new { x.Status, x.DueAtUtc });
        builder.HasIndex(x => new { x.CheckedOutAtUtc, x.Id });
        builder.HasOne(x => x.Member).WithMany(x => x.Loans).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BookCopy).WithMany(x => x.Loans).HasForeignKey(x => x.BookCopyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LoanHistoryConfiguration : IEntityTypeConfiguration<LoanHistory>
{
    public void Configure(EntityTypeBuilder<LoanHistory> builder)
    {
        builder.ToTable("CirculationLoanHistory"); builder.HasKey(x => x.Id);
        builder.Property(x => x.OccurredAtUtc).HasPrecision(0); builder.Property(x => x.ActorUserId).HasMaxLength(450);
        builder.Property(x => x.Notes).HasMaxLength(500); builder.HasIndex(x => new { x.LoanId, x.OccurredAtUtc });
        builder.HasOne(x => x.Loan).WithMany(x => x.History).HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SchoolHolidayConfiguration : IEntityTypeConfiguration<SchoolHoliday>
{
    public void Configure(EntityTypeBuilder<SchoolHoliday> builder)
    {
        builder.ToTable("SchoolHolidays"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired(); builder.HasIndex(x => x.Date).IsUnique();
    }
}
