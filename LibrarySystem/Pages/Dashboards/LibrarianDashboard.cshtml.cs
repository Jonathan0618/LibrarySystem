using LibrarySystem.Application.Dashboard;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ViewOperationalReports)]
public sealed class LibrarianDashboardModel(
    ILibrarianDashboardService dashboardService) : PageModel
{
    public LibrarianDashboardDto Dashboard { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await dashboardService.GetAsync(cancellationToken);
    }
}
