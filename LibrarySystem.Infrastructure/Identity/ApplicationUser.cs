using System.ComponentModel.DataAnnotations;
using LibrarySystem.Infrastructure.Members;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Identity;

[Index(nameof(IsActive))]
public sealed class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    public bool IsActive { get; set; } = true;

    [Precision(0)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Precision(0)]
    public DateTime? UpdatedAtUtc { get; set; }

    public LibraryMember? LibraryMember { get; set; }
}
