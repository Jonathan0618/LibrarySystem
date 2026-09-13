using LibrarySystem.Domain.Catalog;
using LibrarySystem.Domain.Members;

namespace LibrarySystem.Application.Reports;

public sealed class OperationalReportDto
{
    public required IReadOnlyCollection<PopularBookReportDto> PopularBooks { get; init; }
    public required IReadOnlyCollection<InactiveBookReportDto> InactiveBooks { get; init; }
    public required IReadOnlyCollection<ProblemCopyReportDto> LostOrDamagedCopies { get; init; }
    public required IReadOnlyCollection<MemberActivityReportDto> MemberActivity { get; init; }
    public required IReadOnlyCollection<FineBalanceReportDto> FineBalances { get; init; }
}

public sealed class PopularBookReportDto
{
    public long BookId { get; init; }
    public required string Title { get; init; }
    public int CheckoutCount { get; init; }
}

public sealed class InactiveBookReportDto
{
    public long BookId { get; init; }
    public required string Title { get; init; }
    public DateTime? LastCheckedOutAtUtc { get; init; }
}

public sealed class ProblemCopyReportDto
{
    public required string Barcode { get; init; }
    public required string Title { get; init; }
    public BookCopyStatus Status { get; init; }
}

public sealed class MemberActivityReportDto
{
    public long MemberId { get; init; }
    public required string MemberNumber { get; init; }
    public required string MemberName { get; init; }
    public MemberType MemberType { get; init; }
    public int CheckoutCount { get; init; }
    public int ReturnCount { get; init; }
    public int ReservationCount { get; init; }
}

public sealed class FineBalanceReportDto
{
    public long MemberId { get; init; }
    public required string MemberNumber { get; init; }
    public required string MemberName { get; init; }
    public decimal Balance { get; init; }
}
