using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ResetPasswordModel(IIdentityRecoveryService recoveryService) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public IActionResult OnGet(string? userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code)) return RedirectToPage("./ForgotPassword");
        Input.UserId = userId; Input.Code = code; return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        var result = await recoveryService.ResetPasswordAsync(Input.UserId, Input.Code, Input.Password, cancellationToken);
        if (result.Succeeded) return RedirectToPage("./ResetPasswordConfirmation");
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error);
        return Page();
    }
    public sealed class InputModel
    {
        [Required] public string UserId { get; set; } = string.Empty;
        [Required] public string Code { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 12)] public string Password { get; set; } = string.Empty;
        [DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = string.Empty;
    }
}
