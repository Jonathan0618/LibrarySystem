namespace LibrarySystem.Application.Dashboard;

public sealed class LibrarianDashboardDto
{
    public required string LibrarianName { get; init; }
    public int CheckedOutToday { get; init; }
    public int OverdueItems { get; init; }
    public int NewMembersThisWeek { get; init; }
    public int CatalogItems { get; init; }
    public decimal OutstandingFineBalance { get; init; }
    public int WeeklyCheckouts { get; init; }
    public int WeeklyReturns { get; init; }
    public int WeeklyReservations { get; init; }
    public int WeeklyNewTitles { get; init; }
    public required IReadOnlyCollection<DashboardActivityDto> RecentActivity { get; init; }
    public required IReadOnlyCollection<OverdueLoanDto> OverdueLoans { get; init; }
}
