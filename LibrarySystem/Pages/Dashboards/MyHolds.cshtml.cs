using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Reservations;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ReserveBooks)]
public sealed class MyHoldsModel(IReservationService reservationService) : PageModel
{
    public IReadOnlyCollection<ReservationDto> Holds { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken cancellationToken) => Holds = await reservationService.GetForCurrentMemberAsync(cancellationToken);
    public async Task<IActionResult> OnPostCancelAsync(long id, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            if (!await reservationService.CancelForCurrentMemberAsync(id, Convert.FromBase64String(rowVersion), cancellationToken)) return NotFound();
            TempData["StatusMessage"] = "The hold was cancelled.";
        }
        catch (FormatException) { return BadRequest("The hold concurrency token is invalid."); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "The hold changed. Review it and try again."; }
        return RedirectToPage();
    }
}
