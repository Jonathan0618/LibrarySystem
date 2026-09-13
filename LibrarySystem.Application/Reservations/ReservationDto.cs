using LibrarySystem.Domain.Reservations;

namespace LibrarySystem.Application.Reservations;

public sealed class ReservationDto
{
    public long Id { get; init; }
    public long BookId { get; init; }
    public required string BookTitle { get; init; }
    public long MemberId { get; init; }
    public required string MemberNumber { get; init; }
    public long? AssignedBookCopyId { get; init; }
    public string? AssignedBarcode { get; init; }
    public ReservationStatus Status { get; init; }
    public int QueuePosition { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public required byte[] RowVersion { get; init; }
}
