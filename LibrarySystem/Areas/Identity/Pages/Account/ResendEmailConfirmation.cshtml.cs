using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LibrarySystem.Infrastructure.Identity;
namespace LibrarySystem.Areas.Identity.Pages.Account;
[AllowAnonymous]
public sealed class ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IIdentityRecoveryService recoveryService) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool Submitted { get; private set; }
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        var user = await userManager.FindByEmailAsync(Input.Email.Trim());
        if (user is not null)
        {
            await recoveryService.QueueEmailConfirmationAsync(
                user.Id,
                renewToken: true,
                cancellationToken: cancellationToken);
        }
        Submitted = true;
        ModelState.Clear();
        return Page();
    }
    public sealed class InputModel { [Required, EmailAddress, SchoolEmailAddress, MaxLength(256), Display(Name = "School email")] public string Email { get; set; } = string.Empty; }
}
