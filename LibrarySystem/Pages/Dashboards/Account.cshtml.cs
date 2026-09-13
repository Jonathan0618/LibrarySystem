using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Dashboard;
using LibrarySystem.Application.Identity;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ReserveBooks)]
public sealed class AccountModel(
    IStudentDashboardService dashboardService,
    IIdentityRecoveryService recoveryService,
    ICurrentUser currentUser) : PageModel
{
    public StudentDashboardDto Dashboard { get; private set; } = null!;

    [BindProperty]
    public EmailInput Input { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Dashboard = await dashboardService.GetAsync(cancellationToken);

    public async Task<IActionResult> OnPostChangeEmailAsync(CancellationToken cancellationToken)
    {
        Dashboard = await dashboardService.GetAsync(cancellationToken);
        if (!ModelState.IsValid) return Page();
        if (currentUser.UserId is null) return Challenge();

        var result = await recoveryService.QueueEmailChangeAsync(
            currentUser.UserId,
            Input.NewEmail,
            cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        TempData["StatusMessage"] = "A confirmation link was sent to the new address. Your current email remains active until the link is confirmed.";
        return RedirectToPage();
    }

    public sealed class EmailInput
    {
        [Required, EmailAddress, SchoolEmailAddress, MaxLength(256)]
        [Display(Name = "New email address")]
        public string NewEmail { get; set; } = string.Empty;
    }
}
