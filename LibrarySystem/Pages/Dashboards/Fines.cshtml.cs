using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Fines;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageCirculation)]
public sealed class FinesModel(IFineService fineService) : PageModel
{
    [BindProperty(SupportsGet=true), MaxLength(200)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet=true)] public bool OutstandingOnly { get; set; } = true;
    public IReadOnlyCollection<FineDto> Fines { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken cancellationToken) => Fines = await fineService.SearchAsync(SearchTerm, OutstandingOnly, cancellationToken);
    public async Task<IActionResult> OnPostActionAsync(long fineId, string rowVersion, decimal amount, string reason, string action, CancellationToken cancellationToken)
    {
        try
        {
            var request = new FineActionRequest { Amount=amount, Reason=reason, RowVersion=Convert.FromBase64String(rowVersion) };
            var result = action switch
            {
                "payment" => await fineService.RecordPaymentAsync(fineId, request, cancellationToken),
                "adjustment" => await fineService.AdjustAsync(fineId, request, cancellationToken),
                "waiver" => await fineService.WaiveAsync(fineId, request, cancellationToken),
                _ => throw new ValidationException("The selected fine action is invalid.")
            };
            if (result is null) return NotFound();
            TempData["StatusMessage"] = $"The {action} was recorded for fine #{fineId}.";
        }
        catch(FormatException){return BadRequest("The fine concurrency token is invalid.");}
        catch(ValidationException exception){TempData["ErrorMessage"]=exception.Message;}
        catch(DbUpdateConcurrencyException){TempData["ErrorMessage"]="The fine changed. Review it and try again.";}
        return RedirectToPage(new { SearchTerm, OutstandingOnly });
    }
}
