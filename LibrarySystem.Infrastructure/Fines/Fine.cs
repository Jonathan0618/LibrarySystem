using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Infrastructure.Circulation;
using LibrarySystem.Infrastructure.Members;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Fines;

[Table("Fines")]
[Index(nameof(LoanId), nameof(Type), IsUnique = true)]
[Index(nameof(MemberId), nameof(Status))]
[Index(nameof(Status), nameof(MemberId))]
public sealed class Fine
{
    [Key] public long Id { get; set; }
    public long MemberId { get; set; }
    [ForeignKey(nameof(MemberId)), DeleteBehavior(DeleteBehavior.Restrict)] public required LibraryMember Member { get; set; }
    public long? LoanId { get; set; }
    [ForeignKey(nameof(LoanId)), DeleteBehavior(DeleteBehavior.Restrict)] public Loan? Loan { get; set; }
    [EnumDataType(typeof(FineType))] public FineType Type { get; set; }
    [Precision(10, 2), Range(typeof(decimal), "0", "100000")] public decimal AssessedAmount { get; set; }
    [Precision(10, 2), Range(typeof(decimal), "0", "100000")] public decimal Balance { get; set; }
    [EnumDataType(typeof(FineStatus))] public FineStatus Status { get; set; } = FineStatus.Outstanding;
    [MaxLength(500)] public required string Reason { get; set; }
    [Precision(0)] public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [Precision(0)] public DateTime? UpdatedAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public ICollection<FineTransaction> Transactions { get; set; } = [];
}
