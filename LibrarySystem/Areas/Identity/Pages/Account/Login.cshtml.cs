using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Common;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel( SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; private set; } = "/";

    [TempData]
    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        ReturnUrl = NormalizeReturnUrl(returnUrl);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = NormalizeReturnUrl(returnUrl);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            Input.Email.Trim(),
            Input.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(Input.Email.Trim());

            if (user?.MustChangePassword == true)
            {
                return RedirectToPage("/Account/ChangePassword", new { area = "Identity", returnUrl = ReturnUrl });
            }

            // Only honor an explicit returnUrl (e.g. someone was redirected here
            // while trying to open a specific deep-linked page).
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                returnUrl != "/" &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Otherwise, send them to the dashboard appropriate for their role.
            var isStaff = user is not null &&
                (await userManager.IsInRoleAsync(user, "Librarian")
                    || await userManager.IsInRoleAsync(user, "Administrator"));

            var homePage = isStaff ? "/Dashboards/LibrarianDashboard" : "/Dashboards/StudentDashboard";
            return LocalRedirect(Url.Page(homePage) ?? homePage);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "This account is temporarily locked. Please try again later or contact the librarian.");
            return Page();
        }

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(
                string.Empty,
                "This account is not currently allowed to sign in. Please contact the librarian.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "The email address or password is incorrect.");
        return Page();
    }

    private string NormalizeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            returnUrl != "/" &&
            Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        // No safe explicit returnUrl was provided. The role-based fallback
        // destination is decided in OnPostAsync, once we know whether sign-in
        // succeeded and who actually signed in.
        return "/";
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress, SchoolEmailAddress]
        [MaxLength(256)]
        [Display(Name = "School email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
