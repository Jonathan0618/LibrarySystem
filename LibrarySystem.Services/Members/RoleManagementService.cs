using System.ComponentModel.DataAnnotations;
using System.Data;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Security;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Identity;
using LibrarySystem.Domain.Members;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Members;

public sealed class RoleManagementService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, ICurrentUser currentUser) : IRoleManagementService
{
    public async Task<IReadOnlyList<MemberRoleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        var users = await userManager.Users.AsNoTracking().OrderBy(item => item.LastName).ThenBy(item => item.FirstName).ToListAsync(cancellationToken);
        var result = new List<MemberRoleDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(new MemberRoleDto
            {
                UserId = user.Id, Email = user.Email ?? string.Empty, DisplayName = $"{user.FirstName} {user.LastName}",
                Roles = (await userManager.GetRolesAsync(user)).OrderBy(item => item).ToArray(), IsActive = user.IsActive
            });
        }
        return result;
    }

    public async Task SetRoleAsync(string userId, string roleName, bool assigned, CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        if (!RoleNames.All.Contains(roleName, StringComparer.Ordinal)) throw new ValidationException("The selected role is invalid.");
        var user = await userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException("The user account was not found.");
        var hasRole = await userManager.IsInRoleAsync(user, roleName);
        if (hasRole == assigned) return;
        var memberRoleNames = new[] { RoleNames.Student, RoleNames.Teacher, RoleNames.Librarian };
        var isMemberRole = memberRoleNames.Contains(roleName, StringComparer.Ordinal);
        var member = isMemberRole
            ? await dbContext.LibraryMembers.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            : null;
        if (isMemberRole && member is null)
            throw new ValidationException("Student, teacher, and librarian roles require a library member profile.");
        if (isMemberRole && !assigned)
            throw new ValidationException("A member's primary role cannot be removed. Assign the replacement role instead.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (!assigned && roleName == RoleNames.Administrator)
        {
            var administrators = await userManager.GetUsersInRoleAsync(RoleNames.Administrator);
            if (administrators.Count <= 1) throw new ValidationException("The last administrator role cannot be removed.");
        }
        if (isMemberRole)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            var previousMemberRoles = currentRoles.Intersect(memberRoleNames).ToArray();
            if (previousMemberRoles.Length > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, previousMemberRoles);
                if (!removeResult.Succeeded) throw new ValidationException(string.Join("; ", removeResult.Errors.Select(item => item.Description)));
            }
            member!.MemberType = roleName switch
            {
                RoleNames.Student => MemberType.Student,
                RoleNames.Teacher => MemberType.Teacher,
                RoleNames.Librarian => MemberType.Librarian,
                _ => throw new ValidationException("The selected role is invalid.")
            };
            member.UpdatedAtUtc = DateTime.UtcNow;
        }
        var result = assigned ? await userManager.AddToRoleAsync(user, roleName) : await userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded) throw new ValidationException(string.Join("; ", result.Errors.Select(item => item.Description)));
        await userManager.UpdateSecurityStampAsync(user);
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId, Action = assigned ? "RoleAssigned" : "RoleRemoved",
            TargetType = nameof(ApplicationUser), TargetId = user.Id, Details = $"Role: {roleName}"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private void EnsureAdministrator()
    {
        if (!currentUser.IsInRole(RoleNames.Administrator)) throw new UnauthorizedAccessException("Only administrators can manage roles.");
    }
}
