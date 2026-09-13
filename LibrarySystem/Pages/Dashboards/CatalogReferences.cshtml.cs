using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageCatalog)]
public sealed class CatalogReferencesModel(ICatalogReferenceService references) : PageModel
{
    [BindProperty] public ReferenceInput Input { get; set; } = new();
    public IReadOnlyCollection<CatalogReferenceDto> Authors { get; private set; }=[];
    public IReadOnlyCollection<CatalogReferenceDto> Categories { get; private set; }=[];
    public IReadOnlyCollection<CatalogReferenceDto> Publishers { get; private set; }=[];
    public IReadOnlyCollection<CatalogReferenceDto> Shelves { get; private set; }=[];

    public async Task OnGetAsync(CancellationToken token) => await LoadAsync(token);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken token)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (Input.Id == 0)
                {
                    _ = Input.Type switch
                    {
                        "author" => await references.AddAuthorAsync(Input.Name, token),
                        "category" => await references.AddCategoryAsync(Input.Name, token),
                        "publisher" => await references.AddPublisherAsync(Input.Name, token),
                        "shelf" => await references.AddShelfAsync(Input.Name, Input.Description, token),
                        _ => throw new ValidationException("The reference type is invalid.")
                    };
                }
                else if (await references.UpdateAsync(Input.Type, Input.Id, Input.Name, Input.Description, token) is null) return NotFound();
                TempData["StatusMessage"] = Input.Id == 0 ? "The reference value was added." : "The reference value was updated.";
                return RedirectToPage();
            }
            catch (ValidationException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
            catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "The reference value could not be saved because it conflicts with existing data."); }
        }
        await LoadAsync(token); return Page();
    }

    public async Task<IActionResult> OnPostActiveAsync(string type, int id, bool active, CancellationToken token)
    {
        try
        {
            if (!await references.SetActiveAsync(type, id, active, token)) return NotFound();
            TempData["StatusMessage"] = active ? "The reference value was restored." : "The reference value was archived. Existing book links were preserved.";
        }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken token)
    {
        Authors=await references.GetAuthorsAsync(token); Categories=await references.GetCategoriesAsync(token);
        Publishers=await references.GetPublishersAsync(token); Shelves=await references.GetShelvesAsync(token);
    }

    public sealed class ReferenceInput
    {
        public int Id { get; set; }
        [Required, RegularExpression("author|category|publisher|shelf")] public string Type { get; set; }="author";
        [Required, MaxLength(200)] public string Name { get; set; }=string.Empty;
        [MaxLength(200)] public string? Description { get; set; }
    }
}
