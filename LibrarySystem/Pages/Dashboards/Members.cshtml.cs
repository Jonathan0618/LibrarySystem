using LibrarySystem.Application.Common;
using LibrarySystem.Application.Identity;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageUsers)]
public sealed class MembersModel(
    IMemberService memberService,
    IIdentityRecoveryService identityRecoveryService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public MemberType? MemberType { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<MemberDto> Members { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public Task<IActionResult> OnPostActivateAsync(
        long id,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, true, cancellationToken);

    public Task<IActionResult> OnPostDeactivateAsync(
        long id,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, false, cancellationToken);

    public async Task<IActionResult> OnPostResendConfirmationAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var member = await memberService.GetByIdAsync(id, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        if (member.MemberType == LibrarySystem.Domain.Members.MemberType.Librarian &&
            !User.IsInRole(RoleNames.Administrator))
        {
            return Forbid();
        }

        if (member.IsActive && !member.EmailConfirmed)
        {
            await identityRecoveryService.QueueEmailConfirmationAsync(
                member.UserId,
                renewToken: true,
                cancellationToken: cancellationToken);
        }

        TempData["StatusMessage"] =
            "If this active account still requires confirmation, a new confirmation email has been queued.";
        return RedirectToPage(new { SearchTerm, MemberType, IsActive, PageNumber });
    }

    private async Task<IActionResult> ChangeStatusAsync(
        long id,
        bool desiredStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await memberService.SetActiveStatusAsync(id, desiredStatus, cancellationToken))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = desiredStatus
                ? "The member account was activated."
                : "The member account was deactivated.";
            return RedirectToPage(new
            {
                SearchTerm,
                MemberType,
                IsActive = (bool?)null,
                PageNumber = 1
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToPage(new { SearchTerm, MemberType, IsActive, PageNumber });
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Members = await memberService.SearchAsync(new MemberSearchRequest
        {
            SearchTerm = SearchTerm,
            MemberType = MemberType,
            IsActive = IsActive,
            PageNumber = PageNumber,
            PageSize = 20
        }, cancellationToken);
    }
}
