using LibrarySystem.Domain.Circulation;

namespace LibrarySystem.Application.Circulation;

public sealed class LoanDto
{
    public long Id { get; init; }

    public Guid CheckoutOperationId { get; init; }

    public long MemberId { get; init; }

    public required string MemberNumber { get; init; }

    public long BookCopyId { get; init; }

    public required string Barcode { get; init; }

    public required string BookTitle { get; init; }

    public DateTime CheckedOutAtUtc { get; init; }

    public DateTime DueAtUtc { get; init; }

    public DateTime? ReturnedAtUtc { get; init; }

    public int RenewalCount { get; init; }

    public LoanStatus Status { get; init; }

    public required byte[] RowVersion { get; init; }
}
