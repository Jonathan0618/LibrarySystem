using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageCatalog)]
public sealed class BookDetailsModel(
    ICatalogService catalogService,
    ICatalogReferenceService referenceService,
    IInventoryService inventoryService) : PageModel
{
    [BindProperty(SupportsGet = true)] public long Id { get; set; }
    [BindProperty] public CopyInput Copy { get; set; } = new();
    [BindProperty] public IFormFile? Cover { get; set; }
    [BindProperty] public string? BookRowVersion { get; set; }

    public BookDto Book { get; private set; } = null!;
    public IReadOnlyCollection<CatalogReferenceDto> Shelves { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadAsync(cancellationToken) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAddCopyAsync(CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(Cover));
        ModelState.Remove(nameof(BookRowVersion));
        if (!ModelState.IsValid)
        {
            return await LoadAsync(cancellationToken) ? Page() : NotFound();
        }

        try
        {
            await catalogService.AddCopyAsync(Id, new CreateBookCopyRequest
            {
                Barcode = Copy.Barcode,
                ShelfLocationId = Copy.ShelfLocationId,
                Condition = Copy.Condition,
                AcquisitionDate = Copy.AcquisitionDate,
                AcquisitionPrice = Copy.AcquisitionPrice
            }, cancellationToken);
            TempData["StatusMessage"] = "The physical copy was added.";
            return RedirectToPage(new { Id });
        }
        catch (ValidationException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
        catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "The copy could not be saved. Confirm that the barcode is unique."); }
        return await LoadAsync(cancellationToken) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostCoverAsync(CancellationToken cancellationToken)
    {
        if (Cover is null || Cover.Length == 0)
        {
            TempData["ErrorMessage"] = "Choose a cover image to upload.";
            return RedirectToPage(new { Id });
        }

        try
        {
            await using var stream = Cover.OpenReadStream();
            var result = await catalogService.SetCoverAsync(Id, stream, Cover.FileName,
                Cover.ContentType, Convert.FromBase64String(BookRowVersion ?? string.Empty), cancellationToken);
            if (result is null) return NotFound();
            TempData["StatusMessage"] = "The cover image was updated.";
        }
        catch (FormatException) { return BadRequest("The book concurrency token is invalid."); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "The title changed while the cover was uploading. Try again."; }
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostStatusAsync(long copyId, BookCopyStatus status, string rowVersion, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            if (status == BookCopyStatus.Withdrawn)
                await inventoryService.WithdrawAsync(copyId, reason ?? string.Empty, cancellationToken);
            else if (!await catalogService.SetCopyStatusAsync(copyId, status, Convert.FromBase64String(rowVersion), cancellationToken)) return NotFound();
            TempData["StatusMessage"] = $"The copy is now {status}.";
        }
        catch (FormatException) { return BadRequest("The copy concurrency token is invalid."); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "The copy changed elsewhere. Review it and try again."; }
        return RedirectToPage(new { Id });
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        var book = await catalogService.GetBookAsync(Id, cancellationToken);
        if (book is null) return false;
        Book = book;
        BookRowVersion = Convert.ToBase64String(book.RowVersion);
        Shelves = (await referenceService.GetShelvesAsync(cancellationToken)).Where(item => item.IsActive).ToArray();
        return true;
    }

    public sealed class CopyInput
    {
        [Required, MaxLength(100)] public string Barcode { get; set; } = string.Empty;
        [Display(Name = "Shelf/location")] public int? ShelfLocationId { get; set; }
        public BookCondition Condition { get; set; } = BookCondition.Good;
        [DataType(DataType.Date), Display(Name = "Acquisition date")] public DateOnly? AcquisitionDate { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99"), Display(Name = "Acquisition price")] public decimal? AcquisitionPrice { get; set; }
    }
}
