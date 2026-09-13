using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages;

[AllowAnonymous]
public sealed class IndexModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        if (signInManager.IsSignedIn(User))
        {
            await signInManager.SignOutAsync();
        }

        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }
}
