using LibrarySystem.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace LibrarySystem.Areas.Identity.Pages.Account;
[AllowAnonymous]
public sealed class ConfirmEmailModel(IIdentityRecoveryService recoveryService) : PageModel
{
    public bool Succeeded { get; private set; }
    public string Message { get; private set; } = "The confirmation link is invalid or has expired.";
    public async Task OnGetAsync(string? userId, string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code)) return;
        var result = await recoveryService.ConfirmEmailAsync(userId, code, cancellationToken);
        Succeeded = result.Succeeded;
        Message = Succeeded ? "Thank you for confirming your school email." : "The confirmation link is invalid or has expired.";
    }
}
