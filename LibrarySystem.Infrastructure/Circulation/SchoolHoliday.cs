using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Circulation;

[Index(nameof(Date), IsUnique = true)]
public sealed class SchoolHoliday
{
    [Key]
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    [Required, MaxLength(200)]
    public required string Name { get; set; }
}
