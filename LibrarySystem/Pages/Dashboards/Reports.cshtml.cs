using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Reports;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ViewOperationalReports)]
public sealed class ReportsModel(IReportService reportService, ICatalogReferenceService catalogReferenceService) : PageModel
{
    [BindProperty(SupportsGet = true), DataType(DataType.Date)] public DateTime? FromDate { get; set; }
    [BindProperty(SupportsGet = true), DataType(DataType.Date)] public DateTime? ToDate { get; set; }
    [BindProperty(SupportsGet = true)] public LoanStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public MemberType? MemberType { get; set; }
    [BindProperty(SupportsGet = true), Range(1, int.MaxValue)] public int? CategoryId { get; set; }
    [BindProperty(SupportsGet = true), Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    public CirculationReportDto Report { get; private set; } = null!;
    public OperationalReportDto OperationalReport { get; private set; } = EmptyOperationalReport();
    public IReadOnlyCollection<CatalogReferenceDto> Categories { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = CreateRequest();
            Report = await reportService.GetCirculationReportAsync(request, cancellationToken);
            OperationalReport = await reportService.GetOperationalReportAsync(request, cancellationToken);
            Categories = await catalogReferenceService.GetCategoriesAsync(cancellationToken);
            return Page();
        }
        catch (ValidationException exception) { ModelState.AddModelError(string.Empty, exception.Message); Report = EmptyReport(); Categories = await catalogReferenceService.GetCategoriesAsync(cancellationToken); return Page(); }
    }

    public async Task<IActionResult> OnGetExportAsync(CancellationToken cancellationToken)
    {
        try
        {
            var contents = await reportService.ExportCirculationCsvAsync(CreateRequest(), cancellationToken);
            return File(contents, "text/csv; charset=utf-8", $"circulation-report-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (ValidationException exception) { return BadRequest(exception.Message); }
    }

    private CirculationReportRequest CreateRequest() => new() { FromDate = FromDate, ToDate = ToDate, Status = Status, MemberType = MemberType, CategoryId = CategoryId, PageNumber = PageNumber, PageSize = 50 };
    private static CirculationReportDto EmptyReport() => new() { Items = [], PageNumber = 1, PageSize = 50 };
    private static OperationalReportDto EmptyOperationalReport() => new() { PopularBooks = [], InactiveBooks = [], LostOrDamagedCopies = [], MemberActivity = [], FineBalances = [] };
}
