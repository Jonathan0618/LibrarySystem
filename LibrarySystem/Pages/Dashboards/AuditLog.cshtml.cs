using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Reports;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ViewAuditLog)]
public sealed class AuditLogModel(IAuditLogService auditLogService) : PageModel
{
    [BindProperty(SupportsGet = true), StringLength(200)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true), DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true), DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true), Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    public AuditLogPageDto Results { get; private set; } = EmptyResults();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Results = await auditLogService.SearchAsync(new AuditLogRequest
            {
                SearchTerm = SearchTerm,
                FromDate = FromDate,
                ToDate = ToDate,
                PageNumber = PageNumber,
                PageSize = 50
            }, cancellationToken);
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
    }

    private static AuditLogPageDto EmptyResults() => new()
    {
        PageNumber = 1,
        PageSize = 50,
        Items = []
    };
}
