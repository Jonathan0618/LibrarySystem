using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Reservations;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ReserveBooks)]
public sealed class StudentCatalogModel(
    ICatalogService catalogService,
    IReservationService reservationService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public PagedResult<BookDto> Books { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostPlaceHoldAsync(long bookId, CancellationToken cancellationToken)
    {
        try
        {
            var hold = await reservationService.PlaceForCurrentMemberAsync(bookId, cancellationToken);
            TempData["StatusMessage"] = $"A hold was placed for {hold.BookTitle}. Your queue position is {hold.QueuePosition}.";
        }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToPage(new { SearchTerm, PageNumber });
    }

    private async Task LoadAsync(CancellationToken cancellationToken) =>
        Books = await catalogService.SearchAsync(new CatalogSearchRequest
        {
            SearchTerm = SearchTerm,
            IncludeArchived = false,
            PageNumber = Math.Max(PageNumber, 1),
            PageSize = 20
        }, cancellationToken);
}
