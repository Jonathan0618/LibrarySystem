using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

[Index(nameof(Name), IsUnique = true)]
[Table("CatalogCategories")]
public sealed class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BookCategory> BookCategories { get; set; } = [];
}
