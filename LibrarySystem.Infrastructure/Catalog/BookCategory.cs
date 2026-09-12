using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

[PrimaryKey(nameof(BookId), nameof(CategoryId))]
[Table("CatalogBookCategories")]
public sealed class BookCategory
{
    public long BookId { get; set; }

    [ForeignKey(nameof(BookId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Book Book { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required Category Category { get; set; }
}
