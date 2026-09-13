using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageCatalog)]
public sealed class CatalogModel(
    ICatalogService catalogService,
    ICatalogReferenceService referenceService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? AuthorId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludeArchived { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<BookDto> Books { get; private set; } = null!;

    public IReadOnlyCollection<CatalogReferenceDto> Authors { get; private set; } = [];

    public IReadOnlyCollection<CatalogReferenceDto> Categories { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public Task<IActionResult> OnPostArchiveAsync(
        long id,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ChangeArchivedStatusAsync(id, true, rowVersion, cancellationToken);

    public Task<IActionResult> OnPostRestoreAsync(
        long id,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ChangeArchivedStatusAsync(id, false, rowVersion, cancellationToken);

    private async Task<IActionResult> ChangeArchivedStatusAsync(
        long id,
        bool archive,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        byte[] concurrencyToken;
        try
        {
            concurrencyToken = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            return BadRequest("The catalog concurrency token is invalid.");
        }

        try
        {
            if (!await catalogService.SetBookArchivedAsync(
                id,
                archive,
                concurrencyToken,
                cancellationToken))
            {
                return NotFound();
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["ErrorMessage"] = "This title was changed by another user. Review it and try again.";
            return RedirectToPage(new { SearchTerm, AuthorId, CategoryId, IncludeArchived, PageNumber });
        }

        TempData["StatusMessage"] = archive
            ? "The title was archived."
            : "The title was restored.";
        return RedirectToPage(new { SearchTerm, AuthorId, CategoryId, IncludeArchived, PageNumber });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Books = await catalogService.SearchAsync(new CatalogSearchRequest
        {
            SearchTerm = SearchTerm,
            AuthorId = AuthorId,
            CategoryId = CategoryId,
            IncludeArchived = IncludeArchived,
            PageNumber = PageNumber,
            PageSize = 20
        }, cancellationToken);
        Authors = (await referenceService.GetAuthorsAsync(cancellationToken)).Where(item => item.IsActive).ToArray();
        Categories = (await referenceService.GetCategoriesAsync(cancellationToken)).Where(item => item.IsActive).ToArray();
    }
}
