using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

[Index(nameof(Name), IsUnique = true)]
[Table("CatalogPublishers")]
public sealed class Publisher
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Book> Books { get; set; } = [];
}
