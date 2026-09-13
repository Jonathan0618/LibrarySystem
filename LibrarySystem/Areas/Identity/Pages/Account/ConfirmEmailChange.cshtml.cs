using LibrarySystem.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class ConfirmEmailChangeModel(IIdentityRecoveryService recoveryService) : PageModel
{
    public bool Succeeded { get; private set; }
    public async Task OnGetAsync(string? userId, string? newEmail, string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(code)) return;
        var result = await recoveryService.ConfirmEmailChangeAsync(userId, newEmail, code, cancellationToken);
        Succeeded = result.Succeeded;
    }
}
