using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Domain.Reservations;

namespace LibrarySystem.Application.Members;

public sealed class MemberDetailsDto
{
    public required MemberDto Member { get; init; }
    public required IReadOnlyList<MemberLoanSummaryDto> CurrentLoans { get; init; }
    public required IReadOnlyList<MemberReservationSummaryDto> Reservations { get; init; }
    public required IReadOnlyList<MemberFineSummaryDto> OutstandingFines { get; init; }
    public required IReadOnlyList<MemberActivityDto> RecentActivity { get; init; }
    public int OverdueLoanCount { get; init; }
    public decimal OutstandingBalance { get; init; }
    public bool CanDeactivate => CurrentLoans.Count == 0 &&
        Reservations.Count == 0 && OutstandingBalance == 0;
}

public sealed class MemberLoanSummaryDto
{
    public long Id { get; init; }
    public required string Title { get; init; }
    public required string Barcode { get; init; }
    public DateTime CheckedOutAtUtc { get; init; }
    public DateTime DueAtUtc { get; init; }
    public LoanStatus Status { get; init; }
    public bool IsOverdue { get; init; }
}

public sealed class MemberReservationSummaryDto
{
    public long Id { get; init; }
    public required string Title { get; init; }
    public ReservationStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}

public sealed class MemberFineSummaryDto
{
    public long Id { get; init; }
    public FineType Type { get; init; }
    public required string Reason { get; init; }
    public decimal Balance { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public required byte[] RowVersion { get; init; }
}

public sealed class MemberActivityDto
{
    public required string Description { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
