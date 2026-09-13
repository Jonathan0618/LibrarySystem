using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Dashboard;
using LibrarySystem.Application.Reservations;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Domain.Reservations;
using LibrarySystem.Domain.Notifications;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Dashboard;

public sealed class StudentDashboardService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IReservationService reservationService,
    ICirculationService circulationService) : IStudentDashboardService
{
    public async Task<StudentDashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var member = await dbContext.LibraryMembers.AsNoTracking()
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.UserId == currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("The signed-in user does not have a library membership.");

        if (!member.IsActive || !member.User.IsActive)
        {
            throw new ValidationException("The library membership is inactive.");
        }

        var account = await reservationService.GetLibraryAccountForCurrentMemberAsync(cancellationToken);
        var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var booksRead = await dbContext.Loans.AsNoTracking().CountAsync(
            item => item.MemberId == member.Id && item.Status == LoanStatus.Returned && item.ReturnedAtUtc >= yearStart,
            cancellationToken);
        var fines = await dbContext.Fines.AsNoTracking()
            .Where(item => item.MemberId == member.Id &&
                (item.Status == FineStatus.Outstanding || item.Status == FineStatus.PartiallyPaid))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new StudentFineDto
            {
                Id = item.Id,
                Type = item.Type,
                Status = item.Status,
                Balance = item.Balance,
                Reason = item.Reason
            })
            .ToArrayAsync(cancellationToken);
        var fineBalance = fines.Sum(item => item.Balance);
        var checkoutLimit = await dbContext.LibraryPolicies.AsNoTracking()
            .Where(item => item.MemberType == member.MemberType && item.IsActive)
            .Select(item => (decimal?)item.MaximumOutstandingBalanceForCheckout)
            .SingleOrDefaultAsync(cancellationToken) ?? 0m;
        var activity = await dbContext.QueuedNotifications.AsNoTracking()
            .Where(item => item.RecipientEmail == member.User.Email && item.Type != NotificationType.Account)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(6)
            .Select(item => new StudentActivityDto
            {
                Type = item.Type,
                DeliveryStatus = item.Status,
                Subject = item.Subject,
                OccurredAtUtc = item.CreatedAtUtc
            })
            .ToArrayAsync(cancellationToken);
        var arrivals = await dbContext.Books.AsNoTracking()
            .Where(item => !item.IsArchived)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.Title)
            .Take(4)
            .Select(item => new StudentDashboardBookDto
            {
                Id = item.Id,
                Title = item.Title,
                Author = item.BookAuthors.OrderBy(link => link.Author.Name)
                    .Select(link => link.Author.Name).FirstOrDefault() ?? "Unknown author"
            })
            .ToArrayAsync(cancellationToken);

        return new StudentDashboardDto
        {
            StudentName = $"{member.User.FirstName} {member.User.LastName}".Trim(),
            MemberNumber = member.MemberNumber,
            Grade = member.Grade,
            ActiveLoans = account.ActiveLoans,
            Reservations = account.Reservations
                .Where(item => item.Status is ReservationStatus.Waiting or ReservationStatus.ReadyForPickup)
                .ToArray(),
            NewArrivals = arrivals,
            BooksReadThisYear = booksRead,
            OutstandingFineBalance = fineBalance,
            CheckoutBalanceLimit = checkoutLimit,
            CheckoutBlocked = fineBalance > checkoutLimit,
            Fines = fines,
            Activity = activity
        };
    }

    public async Task<LoanDto?> RenewAsync(
        long loanId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        var memberId = await dbContext.LibraryMembers.AsNoTracking()
            .Where(item => item.UserId == currentUser.UserId && item.IsActive && item.User.IsActive)
            .Select(item => (long?)item.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("An active library membership is required.");
        var ownsLoan = await dbContext.Loans.AsNoTracking()
            .AnyAsync(item => item.Id == loanId && item.MemberId == memberId, cancellationToken);
        if (!ownsLoan)
        {
            throw new UnauthorizedAccessException("Students may renew only their own loans.");
        }

        return await circulationService.RenewAsync(
            loanId,
            new RenewLoanRequest { RowVersion = rowVersion },
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<LoanDto>> GetReadingHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var memberId = await dbContext.LibraryMembers.AsNoTracking()
            .Where(item => item.UserId == currentUser.UserId)
            .Select(item => (long?)item.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("A library membership is required.");

        return await dbContext.Loans.AsNoTracking()
            .Where(item => item.MemberId == memberId && item.Status == LoanStatus.Returned)
            .OrderByDescending(item => item.ReturnedAtUtc)
            .Select(item => new LoanDto
            {
                Id = item.Id,
                CheckoutOperationId = item.CheckoutOperationId,
                MemberId = item.MemberId,
                MemberNumber = item.Member.MemberNumber,
                BookCopyId = item.BookCopyId,
                Barcode = item.BookCopy.Barcode,
                BookTitle = item.BookCopy.Book.Title,
                CheckedOutAtUtc = item.CheckedOutAtUtc,
                DueAtUtc = item.DueAtUtc,
                ReturnedAtUtc = item.ReturnedAtUtc,
                RenewalCount = item.RenewalCount,
                Status = item.Status,
                RowVersion = item.RowVersion
            })
            .ToArrayAsync(cancellationToken);
    }
}
