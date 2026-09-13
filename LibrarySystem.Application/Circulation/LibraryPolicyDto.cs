using LibrarySystem.Domain.Members;

namespace LibrarySystem.Application.Circulation;

public sealed class LibraryPolicyDto
{
    public int Id { get; init; }
    public MemberType MemberType { get; init; }
    public int LoanPeriodDays { get; init; }
    public int MaximumActiveLoans { get; init; }
    public int MaximumRenewals { get; init; }
    public bool AllowRenewalWhenOverdue { get; init; }
    public bool SkipWeekendsAndSchoolHolidays { get; init; }
    public int FineGracePeriodDays { get; init; }
    public decimal DailyOverdueFine { get; init; }
    public decimal MaximumOverdueFine { get; init; }
    public decimal LostItemFine { get; init; }
    public decimal DamagedItemFine { get; init; }
    public decimal MaximumOutstandingBalanceForCheckout { get; init; }
    public required byte[] RowVersion { get; init; }
}
