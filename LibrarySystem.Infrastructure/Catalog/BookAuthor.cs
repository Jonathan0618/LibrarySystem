using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

[PrimaryKey(nameof(BookId), nameof(AuthorId))]
[Table("CatalogBookAuthors")]
public sealed class BookAuthor
{
    public long BookId { get; set; }

    [ForeignKey(nameof(BookId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required Book Book { get; set; }

    public int AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required Author Author { get; set; }
}
