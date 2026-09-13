using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Circulation;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Circulation;

[Table("CirculationLoanHistory")]
[Index(nameof(LoanId), nameof(OccurredAtUtc))]
public sealed class LoanHistory
{
    [Key]
    public long Id { get; set; }

    public long LoanId { get; set; }

    [ForeignKey(nameof(LoanId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Loan Loan { get; set; }

    [EnumDataType(typeof(LoanHistoryAction))]
    public LoanHistoryAction Action { get; set; }

    [Precision(0)]
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? ActorUserId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
