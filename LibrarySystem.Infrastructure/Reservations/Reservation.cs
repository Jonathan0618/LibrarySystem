using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Reservations;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Members;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Reservations;

[Table("Reservations")]
[Index(nameof(BookId), nameof(Status), nameof(CreatedAtUtc))]
[Index(nameof(MemberId), nameof(Status))]
[Index(nameof(AssignedBookCopyId), IsUnique = true)]
public sealed class Reservation
{
    [Key]
    public long Id { get; set; }

    public long BookId { get; set; }

    [ForeignKey(nameof(BookId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required Book Book { get; set; }

    public long MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required LibraryMember Member { get; set; }

    public long? AssignedBookCopyId { get; set; }

    [ForeignKey(nameof(AssignedBookCopyId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public BookCopy? AssignedBookCopy { get; set; }

    [EnumDataType(typeof(ReservationStatus))]
    public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;

    [Precision(0)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Precision(0)]
    public DateTime? ReadyAtUtc { get; set; }

    [Precision(0)]
    public DateTime? ExpiresAtUtc { get; set; }

    [Precision(0)]
    public DateTime? CompletedAtUtc { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
