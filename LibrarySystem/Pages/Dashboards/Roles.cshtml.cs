using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Roles = RoleNames.Administrator)]
public sealed class RolesModel(IRoleManagementService roleManagementService) : PageModel
{
    public IReadOnlyList<MemberRoleDto> Accounts { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken cancellationToken) => Accounts = await roleManagementService.ListAsync(cancellationToken);
    public async Task<IActionResult> OnPostAsync(string userId, string roleName, bool assigned, CancellationToken cancellationToken)
    {
        try { await roleManagementService.SetRoleAsync(userId, roleName, assigned, cancellationToken); TempData["StatusMessage"] = "The account role was updated. Existing sessions were invalidated."; }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        return RedirectToPage();
    }
}
