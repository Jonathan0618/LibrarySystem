using LibrarySystem.Application.Circulation;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Circulation;

public sealed class DueDateCalculator(ApplicationDbContext dbContext) : IDueDateCalculator
{
    public async Task<DateTime> CalculateAsync(
        DateTime checkoutAtUtc,
        int loanPeriodDays,
        bool skipClosedDays,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(loanPeriodDays);

        var startDate = DateOnly.FromDateTime(checkoutAtUtc);
        if (!skipClosedDays)
        {
            return DateTime.SpecifyKind(
                startDate.AddDays(loanPeriodDays).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
        }

        var holidays = await dbContext.SchoolHolidays.AsNoTracking()
            .Where(holiday => holiday.Date > startDate)
            .Select(holiday => holiday.Date)
            .ToHashSetAsync(cancellationToken);

        var dueDate = startDate;
        var eligibleDays = 0;
        while (eligibleDays < loanPeriodDays)
        {
            dueDate = dueDate.AddDays(1);
            if (dueDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidays.Contains(dueDate))
            {
                continue;
            }

            eligibleDays++;
        }

        return DateTime.SpecifyKind(dueDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
    }
}
