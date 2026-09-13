using LibrarySystem.Domain.Catalog;

namespace LibrarySystem.Application.Catalog;

public sealed class BookCopyDto
{
    public long Id { get; init; }

    public long BookId { get; init; }

    public required string Barcode { get; init; }

    public string? ShelfCode { get; init; }

    public int? ShelfLocationId { get; init; }

    public BookCopyStatus Status { get; init; }

    public BookCondition Condition { get; init; }

    public DateOnly? AcquisitionDate { get; init; }

    public decimal? AcquisitionPrice { get; init; }

    public required byte[] RowVersion { get; init; }

    public required IReadOnlyCollection<BookCopyHistoryDto> History { get; init; }
}
