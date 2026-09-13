using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace LibrarySystem.Areas.Identity.Pages.Account;
[AllowAnonymous]
public sealed class AccessDeniedModel : PageModel
{
    public string CorrelationId { get; private set; } = string.Empty;
    public void OnGet() => CorrelationId = HttpContext.TraceIdentifier ?? Activity.Current?.TraceId.ToString() ?? "Unavailable";
}
