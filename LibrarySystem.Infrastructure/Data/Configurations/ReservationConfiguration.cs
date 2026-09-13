using LibrarySystem.Infrastructure.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Infrastructure.Data.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations"); builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0); builder.Property(x => x.ReadyAtUtc).HasPrecision(0);
        builder.Property(x => x.ExpiresAtUtc).HasPrecision(0); builder.Property(x => x.CompletedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.BookId, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.MemberId, x.Status }); builder.HasIndex(x => x.AssignedBookCopyId).IsUnique();
        builder.HasOne(x => x.Book).WithMany(x => x.Reservations).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Member).WithMany(x => x.Reservations).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AssignedBookCopy).WithOne(x => x.AssignedReservation)
            .HasForeignKey<Reservation>(x => x.AssignedBookCopyId).OnDelete(DeleteBehavior.Restrict);
    }
}
