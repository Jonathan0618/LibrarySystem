namespace LibrarySystem.Application.Dashboard;

public sealed class DashboardActivityDto
{
    public required string MemberName { get; init; }
    public required string BookTitle { get; init; }
    public required string Action { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
