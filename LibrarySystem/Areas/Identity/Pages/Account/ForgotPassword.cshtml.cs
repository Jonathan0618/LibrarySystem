using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ForgotPasswordModel(IIdentityRecoveryService recoveryService) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        await recoveryService.QueuePasswordResetAsync(Input.Email, cancellationToken);
        return RedirectToPage("./ForgotPasswordConfirmation");
    }
    public sealed class InputModel
    {
        [Required, EmailAddress, SchoolEmailAddress, MaxLength(256), Display(Name = "School email")]
        public string Email { get; set; } = string.Empty;
    }
}
