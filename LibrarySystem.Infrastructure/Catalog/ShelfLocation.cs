using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

[Index(nameof(Code), IsUnique = true)]
[Table("CatalogShelfLocations")]
public sealed class ShelfLocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Code { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BookCopy> BookCopies { get; set; } = [];
}
