using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Dashboard;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ReserveBooks)]
public sealed class StudentDashboardModel(IStudentDashboardService dashboardService) : PageModel
{
    public StudentDashboardDto Dashboard { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Dashboard = await dashboardService.GetAsync(cancellationToken);

    public async Task<IActionResult> OnPostRenewAsync(long loanId, string rowVersion, CancellationToken cancellationToken)
    {
        byte[] token;
        try { token = Convert.FromBase64String(rowVersion); }
        catch (FormatException) { return BadRequest("The loan concurrency token is invalid."); }

        try
        {
            var loan = await dashboardService.RenewAsync(loanId, token, cancellationToken);
            if (loan is null) return NotFound();
            TempData["StatusMessage"] = $"{loan.BookTitle} was renewed until {loan.DueAtUtc.ToLocalTime():d}.";
        }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "This loan changed before it could be renewed. Review it and try again."; }

        return RedirectToPage();
    }
}
