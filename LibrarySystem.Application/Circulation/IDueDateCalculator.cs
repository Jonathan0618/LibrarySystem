namespace LibrarySystem.Application.Circulation;

public interface IDueDateCalculator
{
    Task<DateTime> CalculateAsync(
        DateTime checkoutAtUtc,
        int loanPeriodDays,
        bool skipClosedDays,
        CancellationToken cancellationToken = default);
}
