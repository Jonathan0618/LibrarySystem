using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Identity;
using LibrarySystem.Application.Notifications;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Members;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Domain.Reservations;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Identity;
using LibrarySystem.Infrastructure.Members;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Members;

public sealed class MemberService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    IAccountNotificationService notificationService,
    IIdentityRecoveryService identityRecoveryService) : IMemberService
{
    public async Task<MemberDto> CreateAsync(
        CreateMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateMemberType(request.MemberType);
        EnsureCanCreateMemberType(request.MemberType);

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
            EmailConfirmed = false,
            MustChangePassword = true
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
        await notificationService.QueueAsync(
            $"account-created:{member.Id}",
            email,
            "Your library account was created",
            $"Hello {firstName}, your school library account ({member.MemberNumber}) has been created. Contact library staff for sign-in instructions.",
            cancellationToken);
        await identityRecoveryService.QueueEmailConfirmationAsync(user.Id, cancellationToken: cancellationToken);
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

        if (member is not null)
        {
            EnsureCanManageMemberType(member.MemberType);
        }
        return member is null ? null : Map(member);
    }

    public async Task<MemberDetailsDto?> GetDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        var member = await dbContext.LibraryMembers.AsNoTracking().Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (member is null) return null;
        EnsureCanManageMemberType(member.MemberType);
        var now = DateTime.UtcNow;
        var loans = await dbContext.Loans.AsNoTracking()
            .Where(item => item.MemberId == id && (item.Status == LoanStatus.Active || item.Status == LoanStatus.Overdue))
            .OrderBy(item => item.DueAtUtc)
            .Select(item => new MemberLoanSummaryDto { Id = item.Id, Title = item.BookCopy.Book.Title, Barcode = item.BookCopy.Barcode, CheckedOutAtUtc = item.CheckedOutAtUtc, DueAtUtc = item.DueAtUtc, Status = item.Status, IsOverdue = item.Status == LoanStatus.Overdue || item.DueAtUtc < now })
            .ToListAsync(cancellationToken);
        var reservations = await dbContext.Reservations.AsNoTracking()
            .Where(item => item.MemberId == id && (item.Status == ReservationStatus.Waiting || item.Status == ReservationStatus.ReadyForPickup))
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new MemberReservationSummaryDto { Id = item.Id, Title = item.Book.Title, Status = item.Status, CreatedAtUtc = item.CreatedAtUtc, ExpiresAtUtc = item.ExpiresAtUtc })
            .ToListAsync(cancellationToken);
        var fines = await dbContext.Fines.AsNoTracking()
            .Where(item => item.MemberId == id && item.Balance > 0 && (item.Status == FineStatus.Outstanding || item.Status == FineStatus.PartiallyPaid))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new MemberFineSummaryDto { Id = item.Id, Type = item.Type, Reason = item.Reason, Balance = item.Balance, CreatedAtUtc = item.CreatedAtUtc, RowVersion = item.RowVersion })
            .ToListAsync(cancellationToken);
        var loanActivity = await dbContext.Loans.AsNoTracking().Where(item => item.MemberId == id)
            .OrderByDescending(item => item.ReturnedAtUtc ?? item.CheckedOutAtUtc).Take(10)
            .Select(item => new MemberActivityDto { Description = item.ReturnedAtUtc.HasValue ? "Returned: " + item.BookCopy.Book.Title : "Checked out: " + item.BookCopy.Book.Title, OccurredAtUtc = item.ReturnedAtUtc ?? item.CheckedOutAtUtc })
            .ToListAsync(cancellationToken);
        var reservationActivity = await dbContext.Reservations.AsNoTracking().Where(item => item.MemberId == id)
            .OrderByDescending(item => item.CompletedAtUtc ?? item.ReadyAtUtc ?? item.CreatedAtUtc).Take(10)
            .Select(item => new MemberActivityDto { Description = "Reservation " + item.Status + ": " + item.Book.Title, OccurredAtUtc = item.CompletedAtUtc ?? item.ReadyAtUtc ?? item.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        return new MemberDetailsDto
        {
            Member = Map(member), CurrentLoans = loans, Reservations = reservations, OutstandingFines = fines,
            RecentActivity = loanActivity.Concat(reservationActivity).OrderByDescending(item => item.OccurredAtUtc).Take(10).ToArray(),
            OverdueLoanCount = loans.Count(item => item.IsOverdue), OutstandingBalance = fines.Sum(item => item.Balance)
        };
    }

    public async Task<PagedResult<MemberDto>> SearchAsync(
        MemberSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = dbContext.LibraryMembers.AsNoTracking();

        if (!currentUser.IsInRole(RoleNames.Administrator))
        {
            query = query.Where(member => member.MemberType != MemberType.Librarian);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(member =>
                member.MemberNumber.Contains(searchTerm) ||
                member.User.FirstName.Contains(searchTerm) ||
                member.User.LastName.Contains(searchTerm));
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
                Email = member.User.Email ?? string.Empty,
                MemberNumber = member.MemberNumber,
                FirstName = member.User.FirstName,
                LastName = member.User.LastName,
                MemberType = member.MemberType,
                Grade = member.Grade,
                Department = member.Department,
                IsActive = member.IsActive,
                EmailConfirmed = member.User.EmailConfirmed,
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

        EnsureCanChangeMemberType(member.MemberType, request.MemberType);

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

        EnsureCanManageMemberType(member.MemberType);

        if (!isActive)
        {
            var obligations = await GetDeactivationObligationsAsync(id, cancellationToken);
            if (obligations.Count > 0)
            {
                throw new ValidationException("This member cannot be deactivated while they have " +
                    string.Join(", ", obligations) + ". Resolve these obligations first.");
            }
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
        if (isActive)
        {
            EnsureSucceeded(
                await userManager.ResetAccessFailedCountAsync(member.User),
                "reset the member access-failure count");
        }
        EnsureSucceeded(
            await userManager.UpdateSecurityStampAsync(member.User),
            "invalidate existing member sessions");
        AddAuditLog(
            isActive ? "MemberActivated" : "MemberDeactivated",
            member,
            null);
        if (!string.IsNullOrWhiteSpace(member.User.Email))
        {
            await notificationService.QueueAsync(
                $"account-status:{member.Id}:{isActive}:{member.UpdatedAtUtc.Value.Ticks}",
                member.User.Email,
                isActive ? "Your library account was activated" : "Your library account was deactivated",
                isActive
                    ? "Your school library account is active."
                    : "Your school library account has been deactivated. Contact library staff if you need assistance.",
                cancellationToken);
        }
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
            Email = member.User.Email ?? string.Empty,
            MemberNumber = member.MemberNumber,
            FirstName = member.User.FirstName,
            LastName = member.User.LastName,
            MemberType = member.MemberType,
            Grade = member.Grade,
            Department = member.Department,
            IsActive = member.IsActive,
            EmailConfirmed = member.User.EmailConfirmed,
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

    private void EnsureCanCreateMemberType(MemberType memberType)
    {
        if (memberType == MemberType.Librarian && !currentUser.IsInRole(RoleNames.Administrator))
        {
            throw new UnauthorizedAccessException("Only an administrator can create a librarian account.");
        }
    }

    private void EnsureCanChangeMemberType(MemberType existingType, MemberType requestedType)
    {
        if (existingType != requestedType && !currentUser.IsInRole(RoleNames.Administrator))
        {
            throw new UnauthorizedAccessException("Only an administrator can change a member's role.");
        }

        EnsureCanManageMemberType(existingType);
        EnsureCanCreateMemberType(requestedType);
    }

    private void EnsureCanManageMemberType(MemberType memberType)
    {
        if (memberType == MemberType.Librarian && !currentUser.IsInRole(RoleNames.Administrator))
        {
            throw new UnauthorizedAccessException("Only an administrator can manage a librarian account.");
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

    private async Task<List<string>> GetDeactivationObligationsAsync(long memberId, CancellationToken cancellationToken)
    {
        var activeLoans = await dbContext.Loans.CountAsync(item => item.MemberId == memberId &&
            (item.Status == LoanStatus.Active || item.Status == LoanStatus.Overdue), cancellationToken);
        var activeReservations = await dbContext.Reservations.CountAsync(item => item.MemberId == memberId &&
            (item.Status == ReservationStatus.Waiting || item.Status == ReservationStatus.ReadyForPickup), cancellationToken);
        var balance = await dbContext.Fines.Where(item => item.MemberId == memberId && item.Balance > 0)
            .SumAsync(item => (decimal?)item.Balance, cancellationToken) ?? 0;
        var obligations = new List<string>();
        if (activeLoans > 0) obligations.Add($"{activeLoans} current loan(s)");
        if (activeReservations > 0) obligations.Add($"{activeReservations} active reservation(s)");
        if (balance > 0) obligations.Add($"an outstanding balance of {CurrencyFormatter.Format(balance)}");
        return obligations;
    }

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
