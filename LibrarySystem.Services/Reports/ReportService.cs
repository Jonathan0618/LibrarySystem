using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Reports;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Catalog;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Infrastructure.Circulation;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Reports;

public sealed class ReportService(ApplicationDbContext dbContext, ICurrentUser currentUser) : IReportService
{
    public async Task<CirculationReportDto> GetCirculationReportAsync(
        CirculationReportRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        Validate(request);
        var query = BuildQuery(request);
        var now = DateTime.UtcNow;
        var totalLoans = await query.CountAsync(cancellationToken);
        var returnedLoans = await query.CountAsync(loan => loan.Status == LoanStatus.Returned, cancellationToken);
        var overdueLoans = await query.CountAsync(loan =>
            (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue) && loan.DueAtUtc < now,
            cancellationToken);
        var uniqueMembers = await query.Select(loan => loan.MemberId).Distinct().CountAsync(cancellationToken);
        var loans = await query
            .OrderByDescending(loan => loan.CheckedOutAtUtc)
            .ThenByDescending(loan => loan.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new CirculationReportDto
        {
            TotalLoans = totalLoans,
            ReturnedLoans = returnedLoans,
            OverdueLoans = overdueLoans,
            UniqueMembers = uniqueMembers,
            Items = loans.Select(Map).ToArray(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<byte[]> ExportCirculationCsvAsync(
        CirculationReportRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        Validate(request);
        var loans = await BuildQuery(request)
            .OrderByDescending(loan => loan.CheckedOutAtUtc)
            .ThenByDescending(loan => loan.Id)
            .Take(10_000)
            .ToListAsync(cancellationToken);
        var csv = new StringBuilder("Loan ID,Member Number,Book Title,Barcode,Checked Out UTC,Due UTC,Returned UTC,Status\r\n");
        foreach (var loan in loans)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                Escape(loan.Id.ToString(CultureInfo.InvariantCulture)),
                Escape(loan.Member.MemberNumber),
                Escape(loan.BookCopy.Book.Title),
                Escape(loan.BookCopy.Barcode),
                Escape(loan.CheckedOutAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                Escape(loan.DueAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                Escape(loan.ReturnedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty),
                Escape(EffectiveStatus(loan).ToString())
            }));
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString());
    }

    public async Task<OperationalReportDto> GetOperationalReportAsync(
        CirculationReportRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        Validate(request);
        var loans = BuildQuery(request);
        var activityFrom = request.FromDate?.Date ?? DateTime.UtcNow.Date.AddDays(-89);
        var activityTo = request.ToDate?.Date.AddDays(1) ?? DateTime.UtcNow.Date.AddDays(1);

        var popularBooks = await loans
            .GroupBy(loan => new { loan.BookCopy.BookId, loan.BookCopy.Book.Title })
            .Select(group => new PopularBookReportDto
            {
                BookId = group.Key.BookId,
                Title = group.Key.Title,
                CheckoutCount = group.Count()
            })
            .OrderByDescending(item => item.CheckoutCount)
            .ThenBy(item => item.Title)
            .Take(10)
            .ToListAsync(cancellationToken);

        var inactiveBooks = await dbContext.Books.AsNoTracking()
            .Where(book => !book.IsArchived)
            .Where(book => !request.CategoryId.HasValue ||
                book.BookCategories.Any(category => category.CategoryId == request.CategoryId.Value))
            .Where(book => !book.Copies.SelectMany(copy => copy.Loans)
                .Any(loan => loan.CheckedOutAtUtc >= activityFrom && loan.CheckedOutAtUtc < activityTo))
            .Select(book => new InactiveBookReportDto
            {
                BookId = book.Id,
                Title = book.Title,
                LastCheckedOutAtUtc = book.Copies.SelectMany(copy => copy.Loans)
                    .Max(loan => (DateTime?)loan.CheckedOutAtUtc)
            })
            .OrderBy(item => item.LastCheckedOutAtUtc)
            .ThenBy(item => item.Title)
            .Take(10)
            .ToListAsync(cancellationToken);

        var problemCopies = await dbContext.BookCopies.AsNoTracking()
            .Where(copy => copy.Status == BookCopyStatus.Lost || copy.Status == BookCopyStatus.Damaged)
            .Where(copy => !request.CategoryId.HasValue ||
                copy.Book.BookCategories.Any(category => category.CategoryId == request.CategoryId.Value))
            .OrderBy(copy => copy.Status)
            .ThenBy(copy => copy.Book.Title)
            .Select(copy => new ProblemCopyReportDto
            {
                Barcode = copy.Barcode,
                Title = copy.Book.Title,
                Status = copy.Status
            })
            .Take(10)
            .ToListAsync(cancellationToken);

        var members = dbContext.LibraryMembers.AsNoTracking()
            .Where(member => !request.MemberType.HasValue || member.MemberType == request.MemberType.Value);
        var memberActivity = await members
            .Select(member => new MemberActivityReportDto
            {
                MemberId = member.Id,
                MemberNumber = member.MemberNumber,
                MemberName = member.User.FirstName + " " + member.User.LastName,
                MemberType = member.MemberType,
                CheckoutCount = member.Loans.Count(loan =>
                    loan.CheckedOutAtUtc >= activityFrom && loan.CheckedOutAtUtc < activityTo &&
                    (!request.CategoryId.HasValue || loan.BookCopy.Book.BookCategories.Any(category =>
                        category.CategoryId == request.CategoryId.Value))),
                ReturnCount = member.Loans.Count(loan =>
                    loan.ReturnedAtUtc >= activityFrom && loan.ReturnedAtUtc < activityTo &&
                    (!request.CategoryId.HasValue || loan.BookCopy.Book.BookCategories.Any(category =>
                        category.CategoryId == request.CategoryId.Value))),
                ReservationCount = member.Reservations.Count(reservation =>
                    reservation.CreatedAtUtc >= activityFrom && reservation.CreatedAtUtc < activityTo &&
                    (!request.CategoryId.HasValue || reservation.Book.BookCategories.Any(category =>
                        category.CategoryId == request.CategoryId.Value)))
            })
            .Where(item => item.CheckoutCount > 0 || item.ReturnCount > 0 || item.ReservationCount > 0)
            .OrderByDescending(item => item.CheckoutCount + item.ReturnCount + item.ReservationCount)
            .ThenBy(item => item.MemberNumber)
            .Take(10)
            .ToListAsync(cancellationToken);

        var fineBalances = await dbContext.Fines.AsNoTracking()
            .Where(fine => fine.Status == FineStatus.Outstanding || fine.Status == FineStatus.PartiallyPaid)
            .Where(fine => !request.MemberType.HasValue || fine.Member.MemberType == request.MemberType.Value)
            .GroupBy(fine => new
            {
                fine.MemberId,
                fine.Member.MemberNumber,
                fine.Member.User.FirstName,
                fine.Member.User.LastName
            })
            .Select(group => new FineBalanceReportDto
            {
                MemberId = group.Key.MemberId,
                MemberNumber = group.Key.MemberNumber,
                MemberName = group.Key.FirstName + " " + group.Key.LastName,
                Balance = group.Sum(fine => fine.Balance)
            })
            .OrderByDescending(item => item.Balance)
            .ThenBy(item => item.MemberNumber)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new OperationalReportDto
        {
            PopularBooks = popularBooks,
            InactiveBooks = inactiveBooks,
            LostOrDamagedCopies = problemCopies,
            MemberActivity = memberActivity,
            FineBalances = fineBalances
        };
    }

    private IQueryable<Loan> BuildQuery(CirculationReportRequest request)
    {
        var query = dbContext.Loans.AsNoTracking()
            .Include(loan => loan.Member)
            .Include(loan => loan.BookCopy).ThenInclude(copy => copy.Book)
            .AsQueryable();
        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(loan => loan.CheckedOutAtUtc >= from);
        }
        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(loan => loan.CheckedOutAtUtc < toExclusive);
        }

        if (request.MemberType.HasValue)
        {
            query = query.Where(loan => loan.Member.MemberType == request.MemberType.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(loan => loan.BookCopy.Book.BookCategories.Any(
                category => category.CategoryId == request.CategoryId.Value));
        }

        var now = DateTime.UtcNow;
        return request.Status switch
        {
            LoanStatus.Active => query.Where(loan => loan.Status == LoanStatus.Active && loan.DueAtUtc >= now),
            LoanStatus.Overdue => query.Where(loan =>
                (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue) && loan.DueAtUtc < now),
            { } status => query.Where(loan => loan.Status == status),
            null => query
        };
    }

    private static LoanDto Map(Loan loan) => new()
    {
        Id = loan.Id,
        CheckoutOperationId = loan.CheckoutOperationId,
        MemberId = loan.MemberId,
        MemberNumber = loan.Member.MemberNumber,
        BookCopyId = loan.BookCopyId,
        Barcode = loan.BookCopy.Barcode,
        BookTitle = loan.BookCopy.Book.Title,
        CheckedOutAtUtc = loan.CheckedOutAtUtc,
        DueAtUtc = loan.DueAtUtc,
        ReturnedAtUtc = loan.ReturnedAtUtc,
        RenewalCount = loan.RenewalCount,
        Status = EffectiveStatus(loan),
        RowVersion = loan.RowVersion
    };

    private static LoanStatus EffectiveStatus(Loan loan) =>
        loan.Status == LoanStatus.Active && loan.DueAtUtc < DateTime.UtcNow
            ? LoanStatus.Overdue
            : loan.Status;

    private static void Validate(CirculationReportRequest request)
    {
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
        {
            throw new ValidationException("The start date cannot be after the end date.");
        }
    }

    private void EnsureStaff()
    {
        if (!currentUser.IsInRole(RoleNames.Administrator) &&
            !currentUser.IsInRole(RoleNames.Librarian))
        {
            throw new UnauthorizedAccessException("Only authorized staff can view operational reports.");
        }
    }

    private static string Escape(string value)
    {
        if (value.Length > 0 && "=+-@".Contains(value[0]))
        {
            value = $"'{value}";
        }
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
