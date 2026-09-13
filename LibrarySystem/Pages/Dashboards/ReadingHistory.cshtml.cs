using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Dashboard;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ReserveBooks)]
public sealed class ReadingHistoryModel(IStudentDashboardService dashboardService) : PageModel
{
    public IReadOnlyCollection<LoanDto> Loans { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken cancellationToken) => Loans = await dashboardService.GetReadingHistoryAsync(cancellationToken);
}
