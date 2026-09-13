using LibrarySystem.Domain.Fines;
namespace LibrarySystem.Application.Fines;
public sealed class FineTransactionDto
{
    public long Id { get; init; }
    public FineTransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public required string Reason { get; init; }
    public string? ActorUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
