using LibrarySystem.Application.Circulation;

namespace LibrarySystem.Application.Reports;

public sealed class CirculationReportDto
{
    public int TotalLoans { get; init; }
    public int ReturnedLoans { get; init; }
    public int OverdueLoans { get; init; }
    public int UniqueMembers { get; init; }
    public required IReadOnlyCollection<LoanDto> Items { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}
