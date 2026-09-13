using LibrarySystem.Application.Dashboard;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Catalog;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Dashboard;

public sealed class LibrarianDashboardService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser) : ILibrarianDashboardService
{
    public async Task<LibrarianDashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-6);

        var recentActivity = await dbContext.LoanHistory.AsNoTracking()
            .OrderByDescending(history => history.OccurredAtUtc)
            .ThenByDescending(history => history.Id)
            .Take(6)
            .Select(history => new DashboardActivityDto
            {
                MemberName = history.Loan.Member.User.FirstName + " " + history.Loan.Member.User.LastName,
                BookTitle = history.Loan.BookCopy.Book.Title,
                Action = history.Action == LoanHistoryAction.CheckedOut ? "Checked out" :
                    history.Action == LoanHistoryAction.Returned ? "Returned" :
                    history.Action == LoanHistoryAction.Renewed ? "Renewed" :
                    history.Action == LoanHistoryAction.MarkedOverdue ? "Marked overdue" : "Marked lost",
                OccurredAtUtc = history.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);

        var overdueLoans = await dbContext.Loans.AsNoTracking()
            .Where(loan =>
                (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue) &&
                loan.DueAtUtc < now)
            .OrderBy(loan => loan.DueAtUtc)
            .Take(5)
            .Select(loan => new OverdueLoanDto
            {
                LoanId = loan.Id,
                MemberName = loan.Member.User.FirstName + " " + loan.Member.User.LastName,
                BookTitle = loan.BookCopy.Book.Title,
                DueAtUtc = loan.DueAtUtc,
                DaysOverdue = EF.Functions.DateDiffDay(loan.DueAtUtc, now)
            })
            .ToListAsync(cancellationToken);

        return new LibrarianDashboardDto
        {
            LibrarianName = await GetLibrarianNameAsync(cancellationToken),
            CheckedOutToday = await dbContext.Loans.AsNoTracking()
                .CountAsync(loan => loan.CheckedOutAtUtc >= today, cancellationToken),
            OverdueItems = await dbContext.Loans.AsNoTracking()
                .CountAsync(loan =>
                    (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue) &&
                    loan.DueAtUtc < now, cancellationToken),
            NewMembersThisWeek = await dbContext.LibraryMembers.AsNoTracking()
                .CountAsync(member => member.CreatedAtUtc >= weekStart, cancellationToken),
            CatalogItems = await dbContext.BookCopies.AsNoTracking()
                .CountAsync(copy => copy.Status != BookCopyStatus.Withdrawn, cancellationToken),
            OutstandingFineBalance = await dbContext.Fines.AsNoTracking()
                .Where(fine => fine.Status == FineStatus.Outstanding || fine.Status == FineStatus.PartiallyPaid)
                .SumAsync(fine => fine.Balance, cancellationToken),
            WeeklyCheckouts = await dbContext.Loans.AsNoTracking()
                .CountAsync(loan => loan.CheckedOutAtUtc >= weekStart, cancellationToken),
            WeeklyReturns = await dbContext.Loans.AsNoTracking()
                .CountAsync(loan => loan.ReturnedAtUtc >= weekStart, cancellationToken),
            WeeklyReservations = await dbContext.Reservations.AsNoTracking()
                .CountAsync(reservation => reservation.CreatedAtUtc >= weekStart, cancellationToken),
            WeeklyNewTitles = await dbContext.Books.AsNoTracking()
                .CountAsync(book => book.CreatedAtUtc >= weekStart, cancellationToken),
            RecentActivity = recentActivity,
            OverdueLoans = overdueLoans
        };
    }

    private async Task<string> GetLibrarianNameAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return "Librarian";
        }

        return await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == currentUser.UserId)
            .Select(user => user.FirstName + " " + user.LastName)
            .SingleOrDefaultAsync(cancellationToken)
            ?? "Librarian";
    }
}
