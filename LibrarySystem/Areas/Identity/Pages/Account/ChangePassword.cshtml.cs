using System.ComponentModel.DataAnnotations;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Areas.Identity.Pages.Account;

[Authorize]
public sealed class ChangePasswordModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool IsFirstLogin { get; private set; }
    public async Task<IActionResult> OnGetAsync(string? returnUrl)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        IsFirstLogin = user.MustChangePassword;
        Input.ReturnUrl = NormalizeReturnUrl(returnUrl);
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        IsFirstLogin = user.MustChangePassword;
        Input.ReturnUrl = NormalizeReturnUrl(Input.ReturnUrl);
        if (!ModelState.IsValid) return Page();
        user.MustChangePassword = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }
        dbContext.AuditLogs.Add(new AuditLog { ActorUserId = user.Id, Action = "PasswordChanged", TargetType = nameof(ApplicationUser), TargetId = user.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        await signInManager.RefreshSignInAsync(user);
        TempData["StatusMessage"] = "Your password was changed successfully.";
        return LocalRedirect(Input.ReturnUrl);
    }
    private string NormalizeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Page("/Dashboards/LibrarianDashboard") ?? "/Dashboards/LibrarianDashboard";
    public sealed class InputModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Current password")] public string CurrentPassword { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 12), Display(Name = "New password")] public string NewPassword { get; set; } = string.Empty;
        [DataType(DataType.Password), Compare(nameof(NewPassword)), Display(Name = "Confirm new password")] public string ConfirmPassword { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = "/";
    }
}
