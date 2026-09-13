using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Dashboard;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ReserveBooks)]
public sealed class MyBooksModel(IStudentDashboardService dashboardService) : PageModel
{
    public StudentDashboardDto Dashboard { get; private set; } = null!;
    public async Task OnGetAsync(CancellationToken cancellationToken) => Dashboard = await dashboardService.GetAsync(cancellationToken);
    public async Task<IActionResult> OnPostRenewAsync(long loanId, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var loan = await dashboardService.RenewAsync(loanId, Convert.FromBase64String(rowVersion), cancellationToken);
            if (loan is null) return NotFound();
            TempData["StatusMessage"] = $"{loan.BookTitle} was renewed until {loan.DueAtUtc.ToLocalTime():d}.";
        }
        catch (FormatException) { return BadRequest("The loan concurrency token is invalid."); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "The loan changed. Review it and try again."; }
        return RedirectToPage();
    }
}
