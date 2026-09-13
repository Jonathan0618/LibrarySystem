namespace LibrarySystem.Application.Dashboard;

public sealed class OverdueLoanDto
{
    public long LoanId { get; init; }
    public required string MemberName { get; init; }
    public required string BookTitle { get; init; }
    public DateTime DueAtUtc { get; init; }
    public int DaysOverdue { get; init; }
}
