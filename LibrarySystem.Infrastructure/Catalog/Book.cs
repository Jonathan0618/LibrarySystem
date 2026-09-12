using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

[Index(nameof(Isbn), IsUnique = true)]
[Index(nameof(Title))]
[Index(nameof(IsArchived))]
[Table("CatalogBooks")]
public sealed class Book
{
    [Key]
    public long Id { get; set; }

    [MaxLength(20)]
    public string? Isbn { get; set; }

    [Required]
    [MaxLength(300)]
    public required string Title { get; set; }

    [MaxLength(100)]
    public string? Edition { get; set; }

    [Range(1000, 9999)]
    public int? PublicationYear { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? CoverImagePath { get; set; }

    public int? PublisherId { get; set; }

    [ForeignKey(nameof(PublisherId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Publisher? Publisher { get; set; }

    public bool IsArchived { get; set; }

    [Precision(0)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Precision(0)]
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];

    public ICollection<BookCategory> BookCategories { get; set; } = [];

    public ICollection<BookCopy> Copies { get; set; } = [];
}
