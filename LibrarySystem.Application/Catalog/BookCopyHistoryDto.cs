namespace LibrarySystem.Application.Catalog;

public sealed class BookCopyHistoryDto
{
    public long LoanId { get; init; }
    public required string MemberNumber { get; init; }
    public DateTime CheckedOutAtUtc { get; init; }
    public DateTime DueAtUtc { get; init; }
    public DateTime? ReturnedAtUtc { get; init; }
    public required string Status { get; init; }
}
