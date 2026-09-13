using LibrarySystem.Infrastructure.Fines;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Infrastructure.Data.Configurations;

public sealed class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable("Fines"); builder.HasKey(x => x.Id);
        builder.Property(x => x.AssessedAmount).HasPrecision(10, 2); builder.Property(x => x.Balance).HasPrecision(10, 2);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0); builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.LoanId, x.Type }).IsUnique();
        builder.HasIndex(x => new { x.MemberId, x.Status }); builder.HasIndex(x => new { x.Status, x.MemberId });
        builder.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Loan).WithMany().HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FineTransactionConfiguration : IEntityTypeConfiguration<FineTransaction>
{
    public void Configure(EntityTypeBuilder<FineTransaction> builder)
    {
        builder.ToTable("FineTransactions"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(10, 2); builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ActorUserId).HasMaxLength(450); builder.Property(x => x.CreatedAtUtc).HasPrecision(0);
        builder.HasIndex(x => new { x.FineId, x.CreatedAtUtc });
        builder.HasOne(x => x.Fine).WithMany(x => x.Transactions).HasForeignKey(x => x.FineId).OnDelete(DeleteBehavior.Cascade);
    }
}
