using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Members;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Members;

[Index(nameof(MemberNumber), IsUnique = true)]
[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(MemberType), nameof(IsActive))]
public sealed class LibraryMember
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    [Required]
    [MaxLength(30)]
    public required string MemberNumber { get; set; }

    [EnumDataType(typeof(MemberType))]
    public MemberType MemberType { get; set; }

    [MaxLength(50)]
    public string? Grade { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public bool IsActive { get; set; } = true;

    [Precision(0)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Precision(0)]
    public DateTime? UpdatedAtUtc { get; set; }

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required ApplicationUser User { get; set; }
}
