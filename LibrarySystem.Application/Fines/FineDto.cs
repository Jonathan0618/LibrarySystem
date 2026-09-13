using LibrarySystem.Domain.Fines;

namespace LibrarySystem.Application.Fines;

public sealed class FineDto
{
    public long Id { get; init; }
    public long MemberId { get; init; }
    public required string MemberNumber { get; init; }
    public string MemberName { get; init; } = string.Empty;
    public long? LoanId { get; init; }
    public FineType Type { get; init; }
    public decimal AssessedAmount { get; init; }
    public decimal Balance { get; init; }
    public FineStatus Status { get; init; }
    public required string Reason { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public required byte[] RowVersion { get; init; }
    public IReadOnlyCollection<FineTransactionDto> Transactions { get; init; } = [];
}
