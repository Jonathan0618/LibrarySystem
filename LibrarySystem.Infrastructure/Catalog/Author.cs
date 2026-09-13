using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

[Index(nameof(Name), IsUnique = true)]
[Table("CatalogAuthors")]
public sealed class Author
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];
}
