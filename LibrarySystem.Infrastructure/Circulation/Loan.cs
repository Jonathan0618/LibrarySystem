using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Members;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Circulation;

[Table("CirculationLoans")]
[Index(nameof(MemberId), nameof(Status))]
[Index(nameof(BookCopyId), nameof(Status))]
[Index(nameof(CheckoutOperationId), nameof(BookCopyId), IsUnique = true)]
[Index(nameof(DueAtUtc), nameof(Status))]
[Index(nameof(Status), nameof(DueAtUtc))]
[Index(nameof(CheckedOutAtUtc), nameof(Id))]
public sealed class Loan
{
    [Key]
    public long Id { get; set; }

    public Guid CheckoutOperationId { get; set; }

    public long MemberId { get; set; }

    [ForeignKey(nameof(MemberId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required LibraryMember Member { get; set; }

    public long BookCopyId { get; set; }

    [ForeignKey(nameof(BookCopyId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required BookCopy BookCopy { get; set; }

    [Precision(0)]
    public DateTime CheckedOutAtUtc { get; set; }

    [Precision(0)]
    public DateTime DueAtUtc { get; set; }

    [Precision(0)]
    public DateTime? ReturnedAtUtc { get; set; }

    [Range(0, 20)]
    public int RenewalCount { get; set; }

    [EnumDataType(typeof(LoanStatus))]
    public LoanStatus Status { get; set; } = LoanStatus.Active;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ICollection<LoanHistory> History { get; set; } = [];
}
