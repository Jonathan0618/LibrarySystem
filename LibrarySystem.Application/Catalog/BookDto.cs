namespace LibrarySystem.Application.Catalog;

public sealed class BookDto
{
    public long Id { get; init; }

    public string? Isbn { get; init; }

    public required string Title { get; init; }

    public string? Edition { get; init; }

    public int? PublicationYear { get; init; }

    public string? Description { get; init; }

    public string? Publisher { get; init; }

    public int? PublisherId { get; init; }

    public string? CoverImagePath { get; init; }

    public required IReadOnlyCollection<string> Authors { get; init; }

    public required IReadOnlyCollection<int> AuthorIds { get; init; }

    public required IReadOnlyCollection<string> Categories { get; init; }

    public required IReadOnlyCollection<int> CategoryIds { get; init; }

    public int TotalCopies { get; init; }

    public int AvailableCopies { get; init; }

    public int OnLoanCopies { get; init; }

    public int ReservedCopies { get; init; }

    public int LostCopies { get; init; }

    public int DamagedCopies { get; init; }

    public int WithdrawnCopies { get; init; }

    public required IReadOnlyCollection<BookCopyDto> Copies { get; init; }

    public bool IsArchived { get; init; }

    public required byte[] RowVersion { get; init; }
}
