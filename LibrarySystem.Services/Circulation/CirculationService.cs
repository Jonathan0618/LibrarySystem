using System.ComponentModel.DataAnnotations;
using System.Data;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Catalog;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Circulation;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Fines;
using LibrarySystem.Services.Reservations;
using LibrarySystem.Domain.Reservations;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Circulation;

public sealed class CirculationService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ReservationService reservationService,
    IDueDateCalculator dueDateCalculator) : ICirculationService
{
    public async Task<IReadOnlyCollection<LoanDto>> CheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OperationId == Guid.Empty)
        {
            throw new ValidationException("A checkout operation ID is required.");
        }

        var existingLoans = await CompleteLoanQuery()
            .AsNoTracking()
            .Where(loan => loan.CheckoutOperationId == request.OperationId)
            .ToListAsync(cancellationToken);
        if (existingLoans.Count > 0)
        {
            if (existingLoans.Any(loan => loan.MemberId != request.MemberId))
            {
                throw new ValidationException("The checkout operation ID has already been used.");
            }

            return existingLoans.Select(Map).ToArray();
        }

        var barcodes = request.Barcodes
            .Select(barcode => barcode.Trim())
            .Where(barcode => !string.IsNullOrWhiteSpace(barcode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (barcodes.Length == 0 || barcodes.Length != request.Barcodes.Count)
        {
            throw new ValidationException("One or more checkout barcodes are empty or duplicated.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var member = await dbContext.LibraryMembers
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == request.MemberId, cancellationToken)
            ?? throw new ValidationException("The selected library member does not exist.");
        if (!member.IsActive || !member.User.IsActive)
        {
            throw new ValidationException("The selected library member is inactive.");
        }

        var policy = await dbContext.LibraryPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.MemberType == member.MemberType && item.IsActive,
                cancellationToken)
            ?? throw new ValidationException("No active circulation policy exists for this member type.");

        var activeLoanCount = await dbContext.Loans.CountAsync(
            loan => loan.MemberId == member.Id &&
                (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue),
            cancellationToken);
        if (activeLoanCount + barcodes.Length > policy.MaximumActiveLoans)
        {
            throw new ValidationException(
                $"This checkout would exceed the member limit of {policy.MaximumActiveLoans} active loans.");
        }

        var outstandingBalance = await dbContext.Fines
            .Where(fine => fine.MemberId == member.Id && fine.Balance > 0)
            .SumAsync(fine => (decimal?)fine.Balance, cancellationToken) ?? 0;
        if (outstandingBalance > policy.MaximumOutstandingBalanceForCheckout)
        {
            throw new ValidationException(
                $"Checkout is blocked because the member's outstanding balance of {CurrencyFormatter.Format(outstandingBalance)} " +
                $"exceeds the allowed limit of {CurrencyFormatter.Format(policy.MaximumOutstandingBalanceForCheckout)}.");
        }

        var copies = await dbContext.BookCopies
            .Include(copy => copy.Book)
            .Where(copy => barcodes.Contains(copy.Barcode))
            .ToListAsync(cancellationToken);
        if (copies.Count != barcodes.Length)
        {
            throw new ValidationException("One or more scanned book copies do not exist.");
        }

        var reservedCopyIds = copies
            .Where(copy => copy.Status == BookCopyStatus.Reserved)
            .Select(copy => copy.Id)
            .ToArray();
        var assignedReservations = await dbContext.Reservations
            .Where(reservation => reservation.MemberId == member.Id &&
                reservation.Status == ReservationStatus.ReadyForPickup &&
                reservation.AssignedBookCopyId.HasValue &&
                reservedCopyIds.Contains(reservation.AssignedBookCopyId.Value))
            .ToListAsync(cancellationToken);
        var assignedCopyIds = assignedReservations
            .Select(reservation => reservation.AssignedBookCopyId!.Value)
            .ToHashSet();
        var unavailableCopy = copies.FirstOrDefault(copy =>
            (copy.Status != BookCopyStatus.Available && !assignedCopyIds.Contains(copy.Id)) ||
            copy.Book.IsArchived);
        if (unavailableCopy is not null)
        {
            throw new ValidationException(
                $"Book copy {unavailableCopy.Barcode} is not available for checkout.");
        }

        var now = DateTime.UtcNow;
        var dueAtUtc = await dueDateCalculator.CalculateAsync(
            now,
            policy.LoanPeriodDays,
            policy.SkipWeekendsAndSchoolHolidays,
            cancellationToken);
        foreach (var copy in copies)
        {
            copy.Status = BookCopyStatus.OnLoan;
            copy.UpdatedAtUtc = now;
            var loan = new Loan
            {
                CheckoutOperationId = request.OperationId,
                Member = member,
                MemberId = member.Id,
                BookCopy = copy,
                BookCopyId = copy.Id,
                CheckedOutAtUtc = now,
                DueAtUtc = dueAtUtc
            };
            loan.History.Add(new LoanHistory
            {
                Loan = loan,
                Action = LoanHistoryAction.CheckedOut,
                OccurredAtUtc = now,
                ActorUserId = currentUser.UserId
            });
            dbContext.Loans.Add(loan);

            var fulfilledReservation = assignedReservations
                .SingleOrDefault(reservation => reservation.AssignedBookCopyId == copy.Id);
            if (fulfilledReservation is not null)
            {
                fulfilledReservation.Status = ReservationStatus.Fulfilled;
                fulfilledReservation.CompletedAtUtc = now;
                fulfilledReservation.AssignedBookCopy = null;
                fulfilledReservation.AssignedBookCopyId = null;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        AddAudit("BooksCheckedOut", nameof(Loan), request.OperationId.ToString(), $"Copies: {copies.Count}");
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var savedLoans = await CompleteLoanQuery()
            .AsNoTracking()
            .Where(loan => loan.CheckoutOperationId == request.OperationId)
            .OrderBy(loan => loan.Id)
            .ToListAsync(cancellationToken);
        return savedLoans.Select(Map).ToArray();
    }

    public async Task<LoanDto?> GetLoanAsync(long id, CancellationToken cancellationToken = default)
    {
        var loan = await CompleteLoanQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return loan is null ? null : Map(loan);
    }

    public async Task<PagedResult<LoanDto>> SearchAsync(
        LoanSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = CompleteLoanQuery().AsNoTracking();
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(loan =>
                loan.Member.MemberNumber.Contains(searchTerm) ||
                loan.Member.User.FirstName.Contains(searchTerm) ||
                loan.Member.User.LastName.Contains(searchTerm) ||
                loan.BookCopy.Barcode.Contains(searchTerm) ||
                loan.BookCopy.Book.Title.Contains(searchTerm));
        }

        query = request.Status switch
        {
            LoanStatus.Active => query.Where(loan => loan.Status == LoanStatus.Active && loan.DueAtUtc >= now),
            LoanStatus.Overdue => query.Where(loan =>
                (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue) &&
                loan.DueAtUtc < now),
            { } status => query.Where(loan => loan.Status == status),
            null => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(loan => loan.CheckedOutAtUtc)
            .ThenByDescending(loan => loan.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LoanDto>
        {
            Items = items.Select(Map).ToArray(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyCollection<LoanDto>> GetActiveLoansForMemberAsync(
        long memberId,
        CancellationToken cancellationToken = default)
    {
        var loans = await CompleteLoanQuery()
            .AsNoTracking()
            .Where(loan => loan.MemberId == memberId &&
                (loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Overdue))
            .OrderBy(loan => loan.DueAtUtc)
            .ToListAsync(cancellationToken);
        return loans.Select(Map).ToArray();
    }

    public async Task<LoanDto?> ReturnAsync(
        long loanId,
        ReturnLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        var loan = await CompleteLoanQuery()
            .SingleOrDefaultAsync(item => item.Id == loanId, cancellationToken);
        if (loan is null)
        {
            return null;
        }

        if (loan.Status == LoanStatus.Returned)
        {
            return Map(loan);
        }

        if (loan.Status == LoanStatus.Lost)
        {
            throw new ValidationException("A lost loan must be resolved before the copy can be returned.");
        }

        if (!Enum.IsDefined(request.Condition))
        {
            throw new ValidationException("The selected return condition is invalid.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Entry(loan).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        var now = DateTime.UtcNow;
        loan.Status = LoanStatus.Returned;
        loan.ReturnedAtUtc = now;
        loan.BookCopy.Condition = request.Condition;
        loan.BookCopy.Status = request.Condition == BookCondition.Damaged
            ? BookCopyStatus.Damaged
            : BookCopyStatus.Available;
        loan.BookCopy.UpdatedAtUtc = now;
        if (request.Condition == BookCondition.Damaged)
        {
            var policy = await GetPolicyAsync(loan, cancellationToken);
            await AssessItemFineAsync(
                loan,
                FineType.DamagedItem,
                policy.DamagedItemFine,
                $"Damaged return for loan #{loan.Id}.",
                now,
                cancellationToken);
        }
        if (loan.BookCopy.Status == BookCopyStatus.Available)
        {
            await reservationService.AssignCopyToNextReservationAsync(
                loan.BookCopy,
                cancellationToken);
        }
        loan.History.Add(new LoanHistory
        {
            Loan = loan,
            Action = LoanHistoryAction.Returned,
            OccurredAtUtc = now,
            ActorUserId = currentUser.UserId,
            Notes = NormalizeNotes(request.Notes)
        });
        AddAudit(
            "BookReturned",
            nameof(Loan),
            loan.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(loan);
    }

    public async Task<LoanDto?> MarkLostAsync(
        long loanId,
        MarkLoanLostRequest request,
        CancellationToken cancellationToken = default)
    {
        var loan = await CompleteLoanQuery()
            .SingleOrDefaultAsync(item => item.Id == loanId, cancellationToken);
        if (loan is null)
        {
            return null;
        }

        if (loan.Status == LoanStatus.Lost)
        {
            return Map(loan);
        }

        if (loan.Status == LoanStatus.Returned)
        {
            throw new ValidationException("A returned loan cannot be marked as lost.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Entry(loan).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        var now = DateTime.UtcNow;
        var policy = await GetPolicyAsync(loan, cancellationToken);
        loan.Status = LoanStatus.Lost;
        loan.BookCopy.Status = BookCopyStatus.Lost;
        loan.BookCopy.UpdatedAtUtc = now;
        loan.History.Add(new LoanHistory
        {
            Loan = loan,
            Action = LoanHistoryAction.MarkedLost,
            OccurredAtUtc = now,
            ActorUserId = currentUser.UserId,
            Notes = NormalizeNotes(request.Notes)
        });
        await AssessItemFineAsync(
            loan,
            FineType.LostItem,
            policy.LostItemFine,
            $"Lost item for loan #{loan.Id}.",
            now,
            cancellationToken);
        AddAudit(
            "LoanMarkedLost",
            nameof(Loan),
            loan.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(loan);
    }

    public async Task<LoanDto?> RenewAsync(
        long loanId,
        RenewLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        var loan = await CompleteLoanQuery()
            .SingleOrDefaultAsync(item => item.Id == loanId, cancellationToken);
        if (loan is null)
        {
            return null;
        }

        if (loan.Status is LoanStatus.Returned or LoanStatus.Lost)
        {
            throw new ValidationException("Only an active loan can be renewed.");
        }

        var policy = await dbContext.LibraryPolicies.AsNoTracking()
            .SingleAsync(
                item => item.MemberType == loan.Member.MemberType && item.IsActive,
                cancellationToken);
        var now = DateTime.UtcNow;
        if (!policy.AllowRenewalWhenOverdue && loan.DueAtUtc < now)
        {
            throw new ValidationException("An overdue loan cannot be renewed.");
        }

        if (loan.RenewalCount >= policy.MaximumRenewals)
        {
            throw new ValidationException("This loan has reached its renewal limit.");
        }

        var hasWaitingReservation = await dbContext.Reservations.AnyAsync(
            reservation => reservation.BookId == loan.BookCopy.BookId &&
                (reservation.Status == ReservationStatus.Waiting ||
                 reservation.Status == ReservationStatus.ReadyForPickup),
            cancellationToken);
        if (hasWaitingReservation)
        {
            throw new ValidationException("This loan cannot be renewed because another member is waiting for the title.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Entry(loan).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        loan.DueAtUtc = await dueDateCalculator.CalculateAsync(
            loan.DueAtUtc > now ? loan.DueAtUtc : now,
            policy.LoanPeriodDays,
            policy.SkipWeekendsAndSchoolHolidays,
            cancellationToken);
        loan.RenewalCount++;
        loan.Status = LoanStatus.Active;
        loan.History.Add(new LoanHistory
        {
            Loan = loan,
            Action = LoanHistoryAction.Renewed,
            OccurredAtUtc = now,
            ActorUserId = currentUser.UserId,
            Notes = $"New due date: {loan.DueAtUtc:O}"
        });
        AddAudit(
            "LoanRenewed",
            nameof(Loan),
            loan.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(loan);
    }

    private IQueryable<Loan> CompleteLoanQuery() => dbContext.Loans
        .Include(loan => loan.Member)
        .Include(loan => loan.BookCopy).ThenInclude(copy => copy.Book)
        .Include(loan => loan.History);

    private Task<LibraryPolicy> GetPolicyAsync(Loan loan, CancellationToken cancellationToken) =>
        dbContext.LibraryPolicies.AsNoTracking().SingleAsync(
            item => item.MemberType == loan.Member.MemberType && item.IsActive,
            cancellationToken);

    private async Task AssessItemFineAsync(
        Loan loan,
        FineType type,
        decimal amount,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (amount <= 0 || dbContext.Fines.Local.Any(fine => fine.LoanId == loan.Id && fine.Type == type) ||
            await dbContext.Fines.AnyAsync(
                fine => fine.LoanId == loan.Id && fine.Type == type,
                cancellationToken))
        {
            return;
        }

        var fine = new Fine
        {
            Member = loan.Member,
            MemberId = loan.MemberId,
            Loan = loan,
            LoanId = loan.Id,
            Type = type,
            AssessedAmount = amount,
            Balance = amount,
            Status = FineStatus.Outstanding,
            Reason = reason,
            CreatedAtUtc = now
        };
        fine.Transactions.Add(new FineTransaction
        {
            Fine = fine,
            Type = FineTransactionType.Charge,
            Amount = amount,
            Reason = reason,
            ActorUserId = currentUser.UserId,
            CreatedAtUtc = now
        });
        dbContext.Fines.Add(fine);
    }

    private void AddAudit(string action, string targetType, string targetId, string? details)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details
        });
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
        Status = loan.Status,
        RowVersion = loan.RowVersion
    };

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var normalized = notes.Trim();
        if (normalized.Length > 500)
        {
            throw new ValidationException("Return notes cannot exceed 500 characters.");
        }

        return normalized;
    }
}
