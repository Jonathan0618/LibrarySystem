using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Fines;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Fines;

[Table("FineTransactions")]
[Index(nameof(FineId), nameof(CreatedAtUtc))]
public sealed class FineTransaction
{
    [Key] public long Id { get; set; }
    public long FineId { get; set; }
    [ForeignKey(nameof(FineId)), DeleteBehavior(DeleteBehavior.Cascade)] public required Fine Fine { get; set; }
    [EnumDataType(typeof(FineTransactionType))] public FineTransactionType Type { get; set; }
    [Precision(10, 2)] public decimal Amount { get; set; }
    [MaxLength(500)] public required string Reason { get; set; }
    [MaxLength(450)] public string? ActorUserId { get; set; }
    [Precision(0)] public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
