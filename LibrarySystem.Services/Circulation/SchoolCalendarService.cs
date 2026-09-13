using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Security;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Circulation;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Circulation;

public sealed class SchoolCalendarService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser) : ISchoolCalendarService
{
    public async Task<IReadOnlyCollection<SchoolHolidayDto>> GetHolidaysAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthorized();
        if (year is < 2000 or > 2200)
        {
            throw new ValidationException("The calendar year must be between 2000 and 2200.");
        }

        var from = new DateOnly(year, 1, 1);
        var to = from.AddYears(1);
        return await dbContext.SchoolHolidays.AsNoTracking()
            .Where(holiday => holiday.Date >= from && holiday.Date < to)
            .OrderBy(holiday => holiday.Date)
            .Select(holiday => new SchoolHolidayDto
            {
                Id = holiday.Id,
                Date = holiday.Date,
                Name = holiday.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SchoolHolidayDto> SaveHolidayAsync(
        SchoolHolidayRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthorized();
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        if (request.Date.Year is < 2000 or > 2200)
        {
            throw new ValidationException("The holiday date must be between the years 2000 and 2200.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var name = request.Name.Trim();
        var holiday = await dbContext.SchoolHolidays
            .SingleOrDefaultAsync(item => item.Date == request.Date, cancellationToken);
        var action = holiday is null ? "SchoolHolidayCreated" : "SchoolHolidayUpdated";
        if (holiday is null)
        {
            holiday = new SchoolHoliday { Date = request.Date, Name = name };
            dbContext.SchoolHolidays.Add(holiday);
        }
        else
        {
            holiday.Name = name;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        AddAudit(action, holiday);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(holiday);
    }

    public async Task<bool> DeleteHolidayAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthorized();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var holiday = await dbContext.SchoolHolidays
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (holiday is null)
        {
            return false;
        }

        dbContext.SchoolHolidays.Remove(holiday);
        AddAudit("SchoolHolidayDeleted", holiday);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private void AddAudit(string action, SchoolHoliday holiday)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = action,
            TargetType = nameof(SchoolHoliday),
            TargetId = holiday.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Details = $"{holiday.Date:yyyy-MM-dd}: {holiday.Name}"
        });
    }

    public async Task<IReadOnlyCollection<LibraryPolicyDto>> GetPoliciesAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthorized();
        return await dbContext.LibraryPolicies.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.MemberType)
            .Select(item => new LibraryPolicyDto { Id = item.Id, MemberType = item.MemberType, LoanPeriodDays = item.LoanPeriodDays, MaximumActiveLoans = item.MaximumActiveLoans, MaximumRenewals = item.MaximumRenewals, AllowRenewalWhenOverdue = item.AllowRenewalWhenOverdue, SkipWeekendsAndSchoolHolidays = item.SkipWeekendsAndSchoolHolidays, FineGracePeriodDays=item.FineGracePeriodDays, DailyOverdueFine=item.DailyOverdueFine, MaximumOverdueFine=item.MaximumOverdueFine, LostItemFine=item.LostItemFine, DamagedItemFine=item.DamagedItemFine, MaximumOutstandingBalanceForCheckout=item.MaximumOutstandingBalanceForCheckout, RowVersion = item.RowVersion })
            .ToListAsync(cancellationToken);
    }

    public async Task<LibraryPolicyDto?> UpdatePolicyAsync(int id, UpdateLibraryPolicyRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthorized();
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (!Enum.IsDefined(request.MemberType)) throw new ValidationException("The selected member type is invalid.");
        var policy = await dbContext.LibraryPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy is null) return null;
        if (policy.MemberType != request.MemberType) throw new ValidationException("The policy member type cannot be changed.");
        dbContext.Entry(policy).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        policy.LoanPeriodDays = request.LoanPeriodDays;
        policy.MaximumActiveLoans = request.MaximumActiveLoans;
        policy.MaximumRenewals = request.MaximumRenewals;
        policy.AllowRenewalWhenOverdue = request.AllowRenewalWhenOverdue;
        policy.SkipWeekendsAndSchoolHolidays = request.SkipWeekendsAndSchoolHolidays;
        policy.FineGracePeriodDays = request.FineGracePeriodDays;
        policy.DailyOverdueFine = request.DailyOverdueFine;
        policy.MaximumOverdueFine = request.MaximumOverdueFine;
        policy.LostItemFine = request.LostItemFine;
        policy.DamagedItemFine = request.DamagedItemFine;
        policy.MaximumOutstandingBalanceForCheckout = request.MaximumOutstandingBalanceForCheckout;
        policy.UpdatedAtUtc = DateTime.UtcNow;
        dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Action = "CirculationPolicyUpdated", TargetType = nameof(LibraryPolicy), TargetId = policy.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), Details = $"{policy.MemberType}: {policy.LoanPeriodDays} days, {policy.MaximumActiveLoans} loans, {policy.MaximumRenewals} renewals, skip closures: {policy.SkipWeekendsAndSchoolHolidays}" });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new LibraryPolicyDto { Id = policy.Id, MemberType = policy.MemberType, LoanPeriodDays = policy.LoanPeriodDays, MaximumActiveLoans = policy.MaximumActiveLoans, MaximumRenewals = policy.MaximumRenewals, AllowRenewalWhenOverdue = policy.AllowRenewalWhenOverdue, SkipWeekendsAndSchoolHolidays = policy.SkipWeekendsAndSchoolHolidays, FineGracePeriodDays=policy.FineGracePeriodDays, DailyOverdueFine=policy.DailyOverdueFine, MaximumOverdueFine=policy.MaximumOverdueFine, LostItemFine=policy.LostItemFine, DamagedItemFine=policy.DamagedItemFine, MaximumOutstandingBalanceForCheckout=policy.MaximumOutstandingBalanceForCheckout, RowVersion = policy.RowVersion };
    }

    private void EnsureAuthorized()
    {
        if (!currentUser.IsInRole(RoleNames.Administrator) && !currentUser.IsInRole(RoleNames.Librarian))
            throw new UnauthorizedAccessException("Only authorized library staff can manage circulation policies and closures.");
    }

    private static SchoolHolidayDto Map(SchoolHoliday holiday) => new()
    {
        Id = holiday.Id,
        Date = holiday.Date,
        Name = holiday.Name
    };
}
