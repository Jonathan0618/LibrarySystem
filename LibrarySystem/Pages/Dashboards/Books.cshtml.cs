using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageCatalog)]
public sealed class BooksModel(
    ICatalogService catalogService,
    ICatalogReferenceService referenceService) : PageModel
{
    [BindProperty(SupportsGet = true), MaxLength(200)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true), Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public long? EditId { get; set; }

    [BindProperty]
    public BookInput Input { get; set; } = new();

    public PagedResult<BookDto> Books { get; private set; } = null!;
    public IReadOnlyCollection<CatalogReferenceDto> Authors { get; private set; } = [];
    public IReadOnlyCollection<CatalogReferenceDto> Categories { get; private set; } = [];
    public IReadOnlyCollection<CatalogReferenceDto> Publishers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        if (!EditId.HasValue)
        {
            return Page();
        }

        var book = await catalogService.GetBookAsync(EditId.Value, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        Input = new BookInput
        {
            Id = book.Id,
            Isbn = book.Isbn,
            Title = book.Title,
            Edition = book.Edition,
            PublicationYear = book.PublicationYear,
            Description = book.Description,
            PublisherId = book.PublisherId,
            AuthorIds = book.AuthorIds.ToArray(),
            CategoryIds = book.CategoryIds.ToArray(),
            RowVersion = Convert.ToBase64String(book.RowVersion)
        };
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            if (Input.Id == 0)
            {
                var created = await catalogService.CreateBookAsync(new CreateBookRequest
                {
                    Isbn = Input.Isbn,
                    Title = Input.Title,
                    Edition = Input.Edition,
                    PublicationYear = Input.PublicationYear,
                    Description = Input.Description,
                    PublisherId = Input.PublisherId,
                    AuthorIds = Input.AuthorIds,
                    CategoryIds = Input.CategoryIds
                }, cancellationToken);
                TempData["StatusMessage"] = "The book was added to the catalog.";
                return RedirectToPage("/Dashboards/BookDetails", new { id = created.Id });
            }
            else
            {
                var rowVersion = Convert.FromBase64String(Input.RowVersion ?? string.Empty);
                var updated = await catalogService.UpdateBookAsync(Input.Id, new UpdateBookRequest
                {
                    Isbn = Input.Isbn,
                    Title = Input.Title,
                    Edition = Input.Edition,
                    PublicationYear = Input.PublicationYear,
                    Description = Input.Description,
                    PublisherId = Input.PublisherId,
                    AuthorIds = Input.AuthorIds,
                    CategoryIds = Input.CategoryIds,
                    RowVersion = rowVersion
                }, cancellationToken);
                if (updated is null)
                {
                    return NotFound();
                }
                TempData["StatusMessage"] = "The book was updated.";
            }

            return RedirectToPage(new { SearchTerm, PageNumber });
        }
        catch (FormatException)
        {
            return BadRequest("The book concurrency token is invalid.");
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "This book was changed by another librarian. Reload it and try again.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The book could not be saved. Check that its ISBN is unique.");
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        long id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await catalogService.SetBookArchivedAsync(
                id,
                true,
                Convert.FromBase64String(rowVersion),
                cancellationToken))
            {
                return NotFound();
            }
            TempData["StatusMessage"] = "The book was removed from the active catalog.";
        }
        catch (FormatException)
        {
            return BadRequest("The book concurrency token is invalid.");
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["ErrorMessage"] = "This book was changed by another librarian. Reload the list and try again.";
        }

        return RedirectToPage(new { SearchTerm, PageNumber });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Books = await catalogService.SearchAsync(new CatalogSearchRequest
        {
            SearchTerm = SearchTerm,
            IncludeArchived = false,
            PageNumber = PageNumber,
            PageSize = 20
        }, cancellationToken);
        Authors = (await referenceService.GetAuthorsAsync(cancellationToken)).Where(item => item.IsActive || Input.AuthorIds.Contains(item.Id)).ToArray();
        Categories = (await referenceService.GetCategoriesAsync(cancellationToken)).Where(item => item.IsActive || Input.CategoryIds.Contains(item.Id)).ToArray();
        Publishers = (await referenceService.GetPublishersAsync(cancellationToken)).Where(item => item.IsActive || Input.PublisherId == item.Id).ToArray();
    }

    public sealed class BookInput
    {
        public long Id { get; set; }

        [MaxLength(20)]
        [Display(Name = "ISBN")]
        public string? Isbn { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Edition { get; set; }

        [Range(1000, 9999)]
        [Display(Name = "Publication year")]
        public int? PublicationYear { get; set; }

        [MaxLength(4000)]
        public string? Description { get; set; }

        [Display(Name = "Publisher")]
        public int? PublisherId { get; set; }

        [Display(Name = "Authors")]
        public int[] AuthorIds { get; set; } = [];

        [Display(Name = "Categories")]
        public int[] CategoryIds { get; set; } = [];

        public string? RowVersion { get; set; }
    }
}
