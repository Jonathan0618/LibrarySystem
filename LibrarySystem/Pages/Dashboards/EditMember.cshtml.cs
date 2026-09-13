using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageUsers)]
public sealed class EditMemberModel(IMemberService memberService) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public MemberDto Member { get; private set; } = null!;
    public bool CanChangeRole => User.IsInRole(RoleNames.Administrator);

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Member = await memberService.GetByIdAsync(id, cancellationToken) ?? null!;
        if (Member is null) return NotFound();
        if (Member.MemberType == MemberType.Librarian && !CanChangeRole) return Forbid();
        Input = new InputModel { FirstName = Member.FirstName, LastName = Member.LastName, MemberType = Member.MemberType, Grade = Member.Grade, Department = Member.Department };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        Member = await memberService.GetByIdAsync(id, cancellationToken) ?? null!;
        if (Member is null) return NotFound();
        if (!ModelState.IsValid) return Page();
        try
        {
            var updated = await memberService.UpdateAsync(id, new UpdateMemberRequest { FirstName = Input.FirstName, LastName = Input.LastName, MemberType = Input.MemberType, Grade = Input.Grade, Department = Input.Department }, cancellationToken);
            if (updated is null) return NotFound();
            TempData["StatusMessage"] = "The member profile was updated.";
            return RedirectToPage("./MemberDetails", new { id });
        }
        catch (ValidationException exception) { ModelState.AddModelError(string.Empty, exception.Message); return Page(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    public sealed class InputModel
    {
        [Required, MaxLength(100), Display(Name="First name")] public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(100), Display(Name="Last name")] public string LastName { get; set; } = string.Empty;
        [Required, EnumDataType(typeof(MemberType)), Display(Name="Member type")] public MemberType MemberType { get; set; }
        [MaxLength(50)] public string? Grade { get; set; }
        [MaxLength(100)] public string? Department { get; set; }
    }
}
