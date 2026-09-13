using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Fines;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageUsers)]
public sealed class MemberDetailsModel(IMemberService memberService, IFineService fineService) : PageModel
{
    public MemberDetailsDto Details { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        try { Details = await memberService.GetDetailsAsync(id, cancellationToken) ?? null!; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        return Details is null ? NotFound() : Page();
    }
    public async Task<IActionResult> OnPostStatusAsync(long id, bool active, CancellationToken cancellationToken)
    {
        try
        {
            if (!await memberService.SetActiveStatusAsync(id, active, cancellationToken)) return NotFound();
            TempData["StatusMessage"] = active ? "The member account was activated." : "The member account was deactivated.";
        }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        return RedirectToPage(new { id });
    }
    public async Task<IActionResult> OnPostResolveFineAsync(long id, long fineId, string rowVersion, decimal amount, string reason, bool waive, CancellationToken cancellationToken)
    {
        try
        {
            var request = new FineActionRequest { Amount = amount, Reason = reason, RowVersion = Convert.FromBase64String(rowVersion) };
            var result = waive ? await fineService.WaiveAsync(fineId, request, cancellationToken) : await fineService.RecordPaymentAsync(fineId, request, cancellationToken);
            if (result is null || result.MemberId != id) return NotFound();
            TempData["StatusMessage"] = waive ? "The fine was waived." : "The payment was recorded.";
        }
        catch (FormatException) { return BadRequest("The fine concurrency token is invalid."); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "The fine changed. Review it and try again."; }
        return RedirectToPage(new { id });
    }
}
