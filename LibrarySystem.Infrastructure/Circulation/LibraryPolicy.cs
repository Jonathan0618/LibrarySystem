using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LibrarySystem.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Circulation;

[Table("CirculationPolicies")]
[Index(nameof(MemberType), IsUnique = true)]
public sealed class LibraryPolicy
{
    [Key]
    public int Id { get; set; }

    [EnumDataType(typeof(MemberType))]
    public MemberType MemberType { get; set; }

    [Range(1, 365)]
    public int LoanPeriodDays { get; set; }

    public bool SkipWeekendsAndSchoolHolidays { get; set; }

    [Range(1, 100)]
    public int MaximumActiveLoans { get; set; }

    [Range(0, 20)]
    public int MaximumRenewals { get; set; }

    [Range(1, 50)]
    public int MaximumActiveReservations { get; set; } = 3;

    [Range(0, 30)]
    public int FineGracePeriodDays { get; set; } = 1;

    [Range(typeof(decimal), "0", "1000")]
    [Precision(10, 2)]
    public decimal DailyOverdueFine { get; set; } = 1.00m;

    [Range(typeof(decimal), "0", "100000")]
    [Precision(10, 2)]
    public decimal MaximumOverdueFine { get; set; } = 100.00m;

    [Range(typeof(decimal), "0", "100000")]
    [Precision(10, 2)]
    public decimal LostItemFine { get; set; } = 500.00m;

    [Range(typeof(decimal), "0", "100000")]
    [Precision(10, 2)]
    public decimal DamagedItemFine { get; set; } = 250.00m;

    [Range(typeof(decimal), "0", "100000")]
    [Precision(10, 2)]
    public decimal MaximumOutstandingBalanceForCheckout { get; set; }

    public bool AllowRenewalWhenOverdue { get; set; }

    public bool IsActive { get; set; } = true;

    [Precision(0)]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Precision(0)]
    public DateTime? UpdatedAtUtc { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
