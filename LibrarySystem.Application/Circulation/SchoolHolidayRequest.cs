using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.Circulation;

public sealed class SchoolHolidayRequest
{
    public DateOnly Date { get; init; }

    [Required, StringLength(200)]
    public required string Name { get; init; }
}
