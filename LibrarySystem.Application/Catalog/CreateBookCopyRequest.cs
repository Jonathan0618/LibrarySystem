using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Catalog;

namespace LibrarySystem.Application.Catalog;

public sealed class CreateBookCopyRequest
{
    [Required]
    [MaxLength(100)]
    public required string Barcode { get; init; }

    public int? ShelfLocationId { get; init; }

    [EnumDataType(typeof(BookCondition))]
    public BookCondition Condition { get; init; } = BookCondition.Good;

    public DateOnly? AcquisitionDate { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? AcquisitionPrice { get; init; }
}
