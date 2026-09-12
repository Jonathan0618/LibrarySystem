using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Members;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Members;

public sealed class MemberService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser) : IMemberService
{
    public async Task<MemberDto> CreateAsync(
        CreateMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateMemberType(request.MemberType);

        var email = request.Email.Trim();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        ValidateRequiredName(firstName, nameof(request.FirstName));
        ValidateRequiredName(lastName, nameof(request.LastName));

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new ValidationException("A user with this email address already exists.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = false
        };

        EnsureSucceeded(
            await userManager.CreateAsync(user, request.TemporaryPassword),
            "create the member account");

        var member = new LibraryMember
        {
            UserId = user.Id,
            User = user,
            MemberNumber = await GenerateMemberNumberAsync(cancellationToken),
            MemberType = request.MemberType,
            Grade = NormalizeOptional(request.Grade),
            Department = NormalizeOptional(request.Department)
        };

        dbContext.LibraryMembers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        EnsureSucceeded(
            await userManager.AddToRoleAsync(user, GetRoleName(request.MemberType)),
            "assign the member role");

        AddAuditLog("MemberCreated", member, $"Member type: {member.MemberType}");
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Map(member);
    }

    public async Task<MemberDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var member = await dbContext.LibraryMembers
            .AsNoTracking()
            .Include(item => item.User)
            .Where(member => member.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        return member is null ? null : Map(member);
    }

    public async Task<PagedResult<MemberDto>> SearchAsync(
        MemberSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = dbContext.LibraryMembers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(member =>
                member.MemberNumber.Contains(searchTerm) ||
                member.User.FirstName.Contains(searchTerm) ||
                member.User.LastName.Contains(searchTerm) ||
                (member.User.Email != null && member.User.Email.Contains(searchTerm)));
        }

        if (request.MemberType.HasValue)
        {
            query = query.Where(member => member.MemberType == request.MemberType.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(member => member.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(member => member.User.LastName)
            .ThenBy(member => member.User.FirstName)
            .ThenBy(member => member.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(member => new MemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                MemberNumber = member.MemberNumber,
                Email = member.User.Email ?? string.Empty,
                FirstName = member.User.FirstName,
                LastName = member.User.LastName,
                MemberType = member.MemberType,
                Grade = member.Grade,
                Department = member.Department,
                IsActive = member.IsActive,
                CreatedAtUtc = member.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<MemberDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<MemberDto?> UpdateAsync(
        long id,
        UpdateMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateMemberType(request.MemberType);

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        ValidateRequiredName(firstName, nameof(request.FirstName));
        ValidateRequiredName(lastName, nameof(request.LastName));

        var member = await dbContext.LibraryMembers
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (member is null)
        {
            return null;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        member.User.FirstName = firstName;
        member.User.LastName = lastName;
        member.User.UpdatedAtUtc = DateTime.UtcNow;
        member.MemberType = request.MemberType;
        member.Grade = NormalizeOptional(request.Grade);
        member.Department = NormalizeOptional(request.Department);
        member.UpdatedAtUtc = DateTime.UtcNow;

        EnsureSucceeded(await userManager.UpdateAsync(member.User), "update the member account");
        await SynchronizeMemberRoleAsync(member.User, request.MemberType);
        AddAuditLog("MemberUpdated", member, $"Member type: {member.MemberType}");
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Map(member);
    }

    public async Task<bool> SetActiveStatusAsync(
        long id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var member = await dbContext.LibraryMembers
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (member is null)
        {
            return false;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        member.IsActive = isActive;
        member.UpdatedAtUtc = DateTime.UtcNow;
        member.User.IsActive = isActive;
        member.User.UpdatedAtUtc = DateTime.UtcNow;

        EnsureSucceeded(
            await userManager.SetLockoutEndDateAsync(
                member.User,
                isActive ? null : DateTimeOffset.MaxValue),
            "update the member lockout status");
        EnsureSucceeded(
            await userManager.UpdateSecurityStampAsync(member.User),
            "invalidate existing member sessions");
        AddAuditLog(
            isActive ? "MemberActivated" : "MemberDeactivated",
            member,
            null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<string> GenerateMemberNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var memberNumber = $"LIB-{DateTime.UtcNow.Year}-{randomPart}";
            var exists = await dbContext.LibraryMembers
                .AnyAsync(member => member.MemberNumber == memberNumber, cancellationToken);
            if (!exists)
            {
                return memberNumber;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique member number.");
    }

    private async Task SynchronizeMemberRoleAsync(
        ApplicationUser user,
        MemberType memberType)
    {
        var assignedRoles = await userManager.GetRolesAsync(user);
        string[] memberRoleNames =
        [
            RoleNames.Student,
            RoleNames.Teacher,
            RoleNames.Librarian
        ];
        var existingMemberRoles = assignedRoles.Intersect(memberRoleNames).ToArray();
        if (existingMemberRoles.Length > 0)
        {
            EnsureSucceeded(
                await userManager.RemoveFromRolesAsync(user, existingMemberRoles),
                "remove the previous member role");
        }

        EnsureSucceeded(
            await userManager.AddToRoleAsync(user, GetRoleName(memberType)),
            "assign the member role");
    }

    private static MemberDto Map(LibraryMember member)
    {
        return new MemberDto
        {
            Id = member.Id,
            UserId = member.UserId,
            MemberNumber = member.MemberNumber,
            Email = member.User.Email ?? string.Empty,
            FirstName = member.User.FirstName,
            LastName = member.User.LastName,
            MemberType = member.MemberType,
            Grade = member.Grade,
            Department = member.Department,
            IsActive = member.IsActive,
            CreatedAtUtc = member.CreatedAtUtc
        };
    }

    private static string GetRoleName(MemberType memberType)
    {
        return memberType switch
        {
            MemberType.Student => RoleNames.Student,
            MemberType.Teacher => RoleNames.Teacher,
            MemberType.Librarian => RoleNames.Librarian,
            _ => throw new ValidationException("The selected member type is invalid.")
        };
    }

    private static void ValidateMemberType(MemberType memberType)
    {
        if (!Enum.IsDefined(memberType))
        {
            throw new ValidationException("The selected member type is invalid.");
        }
    }

    private static void ValidateRequiredName(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{propertyName} is required.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void AddAuditLog(string action, LibraryMember member, string? details)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = action,
            TargetType = nameof(LibraryMember),
            TargetId = member.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Details = details
        });
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new ValidationException($"Unable to {operation}. {errors}");
    }
}
