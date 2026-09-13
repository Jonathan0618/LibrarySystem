using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Members;

namespace LibrarySystem.Application.Circulation;

public sealed class UpdateLibraryPolicyRequest
{
    [EnumDataType(typeof(MemberType))] public MemberType MemberType { get; init; }
    [Range(1, 365)] public int LoanPeriodDays { get; init; }
    [Range(1, 100)] public int MaximumActiveLoans { get; init; }
    [Range(0, 20)] public int MaximumRenewals { get; init; }
    public bool AllowRenewalWhenOverdue { get; init; }
    public bool SkipWeekendsAndSchoolHolidays { get; init; }
    [Range(0,30)] public int FineGracePeriodDays { get; init; }
    [Range(typeof(decimal),"0","1000")] public decimal DailyOverdueFine { get; init; }
    [Range(typeof(decimal),"0","100000")] public decimal MaximumOverdueFine { get; init; }
    [Range(typeof(decimal),"0","100000")] public decimal LostItemFine { get; init; }
    [Range(typeof(decimal),"0","100000")] public decimal DamagedItemFine { get; init; }
    [Range(typeof(decimal),"0","100000")] public decimal MaximumOutstandingBalanceForCheckout { get; init; }
    [Required] public required byte[] RowVersion { get; init; }
}
