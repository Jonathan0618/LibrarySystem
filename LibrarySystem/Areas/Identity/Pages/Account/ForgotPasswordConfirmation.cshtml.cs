using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ForgotPasswordConfirmationModel : PageModel
{
    public void OnGet() { }
}
