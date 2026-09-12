using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

[Index(nameof(Barcode), IsUnique = true)]
[Index(nameof(BookId), nameof(Status))]
[Table("CatalogBookCopies")]
public sealed class BookCopy
{
    [Key]
    public long Id { get; set; }

    public long BookId { get; set; }

    [ForeignKey(nameof(BookId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required Book Book { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Barcode { get; set; }

    public int? ShelfLocationId { get; set; }

    [ForeignKey(nameof(ShelfLocationId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public ShelfLocation? ShelfLocation { get; set; }

    [EnumDataType(typeof(BookCopyStatus))]
    public BookCopyStatus Status { get; set; } = BookCopyStatus.Available;

    [EnumDataType(typeof(BookCondition))]
    public BookCondition Condition { get; set; } = BookCondition.Good;

    [DataType(DataType.Date)]
    public DateOnly? AcquisitionDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? AcquisitionPrice { get; set; }

    [Precision(0)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Precision(0)]
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
