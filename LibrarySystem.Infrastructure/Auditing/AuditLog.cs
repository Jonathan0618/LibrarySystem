using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Auditing;

[Index(nameof(CreatedAtUtc))]
[Index(nameof(TargetType), nameof(TargetId))]
public sealed class AuditLog
{
    [Key]
    public long Id { get; set; }

    [MaxLength(450)]
    public string? ActorUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Action { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TargetType { get; set; }

    [Required]
    [MaxLength(100)]
    public required string TargetId { get; set; }

    [MaxLength(1000)]
    public string? Details { get; set; }

    [Precision(0)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
