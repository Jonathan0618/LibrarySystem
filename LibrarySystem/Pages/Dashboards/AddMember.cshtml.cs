using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageUsers)]
public sealed class AddMemberModel(IMemberService memberService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool CanCreateLibrarian => User.IsInRole(RoleNames.Administrator);

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var member = await memberService.CreateAsync(new CreateMemberRequest
            {
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                TemporaryPassword = Input.TemporaryPassword,
                MemberType = Input.MemberType,
                Grade = Input.Grade,
                Department = Input.Department
            }, cancellationToken);

            TempData["StatusMessage"] =
                $"{member.FirstName} {member.LastName} was added as member {member.MemberNumber}. A confirmation email has been queued.";
            return RedirectToPage("./Members");
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    public sealed class InputModel
    {
        [Required, EmailAddress, SchoolEmailAddress, MaxLength(256)]
        [Display(Name = "School email")]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EnumDataType(typeof(MemberType))]
        [Display(Name = "Member type")]
        public MemberType MemberType { get; set; } = MemberType.Student;

        [MaxLength(50)]
        public string? Grade { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [Required, MinLength(12), MaxLength(100), DataType(DataType.Password)]
        [Display(Name = "Temporary password")]
        public string TemporaryPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(TemporaryPassword), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm temporary password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
