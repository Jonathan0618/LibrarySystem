using LibrarySystem.Application.Common;
using Microsoft.AspNetCore.Identity;

namespace LibrarySystem.Infrastructure.Identity;

public sealed class SchoolEmailUserValidator : IUserValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.Email) &&
            SchoolEmailAddressAttribute.IsSchoolEmail(user.Email))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "InvalidSchoolEmail",
            Description = "The account email must use the @nvsu.edu.ph school domain."
        }));
    }
}
