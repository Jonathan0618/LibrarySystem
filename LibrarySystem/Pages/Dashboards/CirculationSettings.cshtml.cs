using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageCirculation)]
public sealed class CirculationSettingsModel(ISchoolCalendarService calendarService) : PageModel
{
    [BindProperty(SupportsGet = true), Range(2000, 2200)] public int Year { get; set; } = DateTime.Today.Year;
    [BindProperty] public HolidayInput Holiday { get; set; } = new() { Date = DateOnly.FromDateTime(DateTime.Today) };
    public IReadOnlyCollection<LibraryPolicyDto> Policies { get; private set; } = [];
    public IReadOnlyCollection<SchoolHolidayDto> Holidays { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);
    public async Task<IActionResult> OnPostPolicyAsync(int id, MemberType memberType, int loanPeriodDays, int maximumActiveLoans, int maximumRenewals, bool allowRenewalWhenOverdue, bool skipWeekendsAndSchoolHolidays, int fineGracePeriodDays, decimal dailyOverdueFine, decimal maximumOverdueFine, decimal lostItemFine, decimal damagedItemFine, decimal maximumOutstandingBalanceForCheckout, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var result = await calendarService.UpdatePolicyAsync(id, new UpdateLibraryPolicyRequest { MemberType = memberType, LoanPeriodDays = loanPeriodDays, MaximumActiveLoans = maximumActiveLoans, MaximumRenewals = maximumRenewals, AllowRenewalWhenOverdue = allowRenewalWhenOverdue, SkipWeekendsAndSchoolHolidays = skipWeekendsAndSchoolHolidays, FineGracePeriodDays=fineGracePeriodDays, DailyOverdueFine=dailyOverdueFine, MaximumOverdueFine=maximumOverdueFine, LostItemFine=lostItemFine, DamagedItemFine=damagedItemFine, MaximumOutstandingBalanceForCheckout=maximumOutstandingBalanceForCheckout, RowVersion = Convert.FromBase64String(rowVersion) }, cancellationToken);
            if (result is null) return NotFound();
            TempData["StatusMessage"] = $"The {memberType} circulation policy was updated.";
        }
        catch (FormatException) { return BadRequest("The policy concurrency token is invalid."); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "The policy changed. Review it and try again."; }
        return RedirectToPage(new { Year });
    }
    public async Task<IActionResult> OnPostHolidayAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await LoadAsync(cancellationToken); return Page(); }
        try { await calendarService.SaveHolidayAsync(new SchoolHolidayRequest { Date = Holiday.Date, Name = Holiday.Name }, cancellationToken); TempData["StatusMessage"] = "The closure date was saved."; }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToPage(new { Year = Holiday.Date.Year });
    }
    public async Task<IActionResult> OnPostDeleteHolidayAsync(int id, CancellationToken cancellationToken)
    {
        if (!await calendarService.DeleteHolidayAsync(id, cancellationToken)) return NotFound();
        TempData["StatusMessage"] = "The closure date was removed.";
        return RedirectToPage(new { Year });
    }
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Policies = await calendarService.GetPoliciesAsync(cancellationToken);
        Holidays = await calendarService.GetHolidaysAsync(Year, cancellationToken);
    }
    public sealed class HolidayInput
    {
        [DataType(DataType.Date)] public DateOnly Date { get; set; }
        [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    }
}
