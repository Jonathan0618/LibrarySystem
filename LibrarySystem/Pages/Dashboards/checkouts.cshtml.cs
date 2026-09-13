using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Catalog;
using LibrarySystem.Domain.Circulation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageCirculation)]
public sealed class CheckoutsModel(ICirculationService circulationService, IMemberService memberService) : PageModel
{
    [BindProperty(SupportsGet = true), MaxLength(200)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public LoanStatus? Status { get; set; }
    [BindProperty(SupportsGet = true), Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [BindProperty, Required, MaxLength(50), Display(Name = "Member number")] public string CheckoutMemberNumber { get; set; } = string.Empty;
    [BindProperty, Required, MaxLength(1000), Display(Name = "Book barcodes")] public string CheckoutBarcodes { get; set; } = string.Empty;
    public PagedResult<LoanDto> Loans { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostCheckoutAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await LoadAsync(cancellationToken); return Page(); }
        var memberNumber = CheckoutMemberNumber.Trim();
        var matches = await memberService.SearchAsync(new MemberSearchRequest { SearchTerm = memberNumber, IsActive = true, PageNumber = 1, PageSize = 20 }, cancellationToken);
        var member = matches.Items.SingleOrDefault(item => string.Equals(item.MemberNumber, memberNumber, StringComparison.OrdinalIgnoreCase));
        if (member is null)
        {
            ModelState.AddModelError(nameof(CheckoutMemberNumber), "No active member has that member number.");
            await LoadAsync(cancellationToken); return Page();
        }
        var barcodes = CheckoutBarcodes.Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        try
        {
            var loans = await circulationService.CheckoutAsync(new CheckoutRequest { OperationId = Guid.NewGuid(), MemberId = member.Id, Barcodes = barcodes }, cancellationToken);
            TempData["StatusMessage"] = $"Checked out {loans.Count} book(s) to {member.MemberNumber}.";
            return RedirectToPage();
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message); await LoadAsync(cancellationToken); return Page();
        }
    }

    public async Task<IActionResult> OnPostReturnAsync(long loanId, string rowVersion, BookCondition condition, string? notes, CancellationToken cancellationToken) =>
        await ChangeLoanAsync(loanId, rowVersion, "returned", token => circulationService.ReturnAsync(loanId, new ReturnLoanRequest { RowVersion = token, Condition = condition, Notes = notes }, cancellationToken));

    public async Task<IActionResult> OnPostRenewAsync(long loanId, string rowVersion, CancellationToken cancellationToken) =>
        await ChangeLoanAsync(loanId, rowVersion, "renewed", token => circulationService.RenewAsync(loanId, new RenewLoanRequest { RowVersion = token }, cancellationToken));

    public async Task<IActionResult> OnPostLostAsync(long loanId, string rowVersion, string? notes, CancellationToken cancellationToken) =>
        await ChangeLoanAsync(loanId, rowVersion, "marked lost and the applicable obligation assessed", token => circulationService.MarkLostAsync(loanId, new MarkLoanLostRequest { RowVersion = token, Notes = notes }, cancellationToken));

    private async Task<IActionResult> ChangeLoanAsync(long loanId, string rowVersion, string action, Func<byte[], Task<LoanDto?>> operation)
    {
        try
        {
            var token = Convert.FromBase64String(rowVersion);
            if (await operation(token) is null) return NotFound();
            TempData["StatusMessage"] = $"Loan #{loanId} was {action}.";
        }
        catch (FormatException) { return BadRequest("The loan concurrency token is invalid."); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "This loan was changed by another user. Review it and try again."; }
        return RedirectToPage(new { SearchTerm, Status, PageNumber });
    }

    private async Task LoadAsync(CancellationToken cancellationToken) => Loans = await circulationService.SearchAsync(new LoanSearchRequest
    {
        SearchTerm = SearchTerm, Status = Status, PageNumber = PageNumber, PageSize = 20
    }, cancellationToken);
}
