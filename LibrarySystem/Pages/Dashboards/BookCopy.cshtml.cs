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
public sealed class BookCopyModel(ICatalogService catalogService, ICatalogReferenceService referenceService) : PageModel
{
    [BindProperty(SupportsGet = true)] public long Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();
    public BookCopyDto Copy { get; private set; } = null!;
    public IReadOnlyCollection<CatalogReferenceDto> Shelves { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken)) return NotFound();
        Input = new InputModel { Barcode = Copy.Barcode, ShelfLocationId = Copy.ShelfLocationId, Condition = Copy.Condition, AcquisitionDate = Copy.AcquisitionDate, AcquisitionPrice = Copy.AcquisitionPrice, RowVersion = Convert.ToBase64String(Copy.RowVersion) };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var result = await catalogService.UpdateCopyAsync(Id, new UpdateBookCopyRequest { Barcode = Input.Barcode, ShelfLocationId = Input.ShelfLocationId, Condition = Input.Condition, AcquisitionDate = Input.AcquisitionDate, AcquisitionPrice = Input.AcquisitionPrice, RowVersion = Convert.FromBase64String(Input.RowVersion) }, cancellationToken);
                if (result is null) return NotFound();
                TempData["StatusMessage"] = "The physical copy was updated.";
                return RedirectToPage("/Dashboards/BookDetails", new { id = result.BookId });
            }
            catch (FormatException) { return BadRequest("The copy concurrency token is invalid."); }
            catch (ValidationException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
            catch (DbUpdateConcurrencyException) { ModelState.AddModelError(string.Empty, "This copy was changed by another librarian. Reload it and try again."); }
            catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "The copy could not be saved. Confirm that the barcode is unique."); }
        }
        return await LoadAsync(cancellationToken) ? Page() : NotFound();
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        var copy = await catalogService.GetCopyAsync(Id, cancellationToken);
        if (copy is null) return false;
        Copy = copy;
        Shelves = (await referenceService.GetShelvesAsync(cancellationToken)).Where(item => item.IsActive || item.Id == copy.ShelfLocationId).ToArray();
        return true;
    }

    public sealed class InputModel
    {
        [Required, MaxLength(100)] public string Barcode { get; set; } = string.Empty;
        [Display(Name="Shelf/location")] public int? ShelfLocationId { get; set; }
        public BookCondition Condition { get; set; } = BookCondition.Good;
        [DataType(DataType.Date), Display(Name="Acquisition date")] public DateOnly? AcquisitionDate { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99"), Display(Name="Acquisition price")] public decimal? AcquisitionPrice { get; set; }
        [Required] public string RowVersion { get; set; } = string.Empty;
    }
}
