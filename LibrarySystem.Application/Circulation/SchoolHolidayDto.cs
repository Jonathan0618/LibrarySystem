namespace LibrarySystem.Application.Circulation;

public sealed class SchoolHolidayDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public required string Name { get; init; }
}
