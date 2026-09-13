using System.ComponentModel.DataAnnotations;
using System.Data;
using LibrarySystem.Application.Reservations;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Catalog;
using LibrarySystem.Domain.Reservations;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Members;
using LibrarySystem.Infrastructure.Reservations;
using LibrarySystem.Infrastructure.Circulation;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Reservations;

public sealed class ReservationService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser) : IReservationService
{
    private const int PickupPeriodDays = 2;

    public async Task<ReservationDto> PlaceForCurrentMemberAsync(
        long bookId,
        CancellationToken cancellationToken = default)
    {
        var member = await GetCurrentMemberAsync(cancellationToken);
        var policy = await dbContext.LibraryPolicies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.MemberType == member.MemberType && item.IsActive, cancellationToken)
            ?? throw new ValidationException("No active library policy exists for this member type.");
        var activeReservationCount = await dbContext.Reservations.CountAsync(
            item => item.MemberId == member.Id &&
                (item.Status == ReservationStatus.Waiting || item.Status == ReservationStatus.ReadyForPickup),
            cancellationToken);
        if (activeReservationCount >= policy.MaximumActiveReservations)
        {
            throw new ValidationException($"This member has reached the limit of {policy.MaximumActiveReservations} active reservations.");
        }
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var book = await dbContext.Books
            .Include(item => item.Copies)
            .SingleOrDefaultAsync(item => item.Id == bookId, cancellationToken)
            ?? throw new ValidationException("The selected book does not exist.");
        if (book.IsArchived)
        {
            throw new ValidationException("An archived title cannot be reserved.");
        }

        if (book.Copies.Any(copy => copy.Status == BookCopyStatus.Available))
        {
            throw new ValidationException("This title currently has an available copy and does not need a reservation.");
        }

        var alreadyReserved = await dbContext.Reservations.AnyAsync(
            reservation => reservation.BookId == bookId && reservation.MemberId == member.Id &&
                (reservation.Status == ReservationStatus.Waiting ||
                 reservation.Status == ReservationStatus.ReadyForPickup),
            cancellationToken);
        if (alreadyReserved)
        {
            throw new ValidationException("This member already has an active reservation for the title.");
        }

        var reservation = new Reservation
        {
            Book = book,
            BookId = book.Id,
            Member = member,
            MemberId = member.Id
        };
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        AddAudit("ReservationPlaced", reservation.Id, $"Book ID: {book.Id}");
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetReservationDtoAsync(reservation.Id, cancellationToken);
    }

    public async Task<bool> CancelForCurrentMemberAsync(
        long reservationId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        var member = await GetCurrentMemberAsync(cancellationToken);
        var reservation = await dbContext.Reservations
            .Include(item => item.AssignedBookCopy)
            .SingleOrDefaultAsync(
                item => item.Id == reservationId && item.MemberId == member.Id,
                cancellationToken);
        if (reservation is null)
        {
            return false;
        }

        if (reservation.Status is ReservationStatus.Cancelled or
            ReservationStatus.Expired or ReservationStatus.Fulfilled)
        {
            return true;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        dbContext.Entry(reservation).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var assignedCopy = reservation.AssignedBookCopy;
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CompletedAtUtc = DateTime.UtcNow;
        reservation.AssignedBookCopy = null;
        reservation.AssignedBookCopyId = null;

        if (assignedCopy is not null)
        {
            await AssignCopyToNextReservationAsync(assignedCopy, cancellationToken);
        }

        AddAudit("ReservationCancelled", reservation.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<ReservationDto>> GetForCurrentMemberAsync(
        CancellationToken cancellationToken = default)
    {
        var member = await GetCurrentMemberAsync(cancellationToken);
        var reservations = await CompleteQuery().AsNoTracking()
            .Where(item => item.MemberId == member.Id)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var results = new List<ReservationDto>(reservations.Count);
        foreach (var reservation in reservations)
        {
            results.Add(await MapAsync(reservation, cancellationToken));
        }

        return results;
    }

    public async Task<MemberLibraryAccountDto> GetLibraryAccountForCurrentMemberAsync(
        CancellationToken cancellationToken = default)
    {
        var member = await GetCurrentMemberAsync(cancellationToken);
        var loans = await dbContext.Loans.AsNoTracking()
            .Include(item => item.Member)
            .Include(item => item.BookCopy).ThenInclude(copy => copy.Book)
            .Where(item => item.MemberId == member.Id &&
                (item.Status == LoanStatus.Active || item.Status == LoanStatus.Overdue))
            .OrderBy(item => item.DueAtUtc)
            .ToListAsync(cancellationToken);

        return new MemberLibraryAccountDto
        {
            ActiveLoans = loans.Select(MapLoan).ToArray(),
            Reservations = await GetForCurrentMemberAsync(cancellationToken)
        };
    }

    public async Task<PagedResult<ReservationDto>> SearchAsync(
        ReservationSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var query = CompleteQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(item => item.Member.MemberNumber.Contains(term) ||
                item.Book.Title.Contains(term) ||
                (item.AssignedBookCopy != null && item.AssignedBookCopy.Barcode.Contains(term)));
        }
        if (request.Status.HasValue)
        {
            query = query.Where(item => item.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var reservations = await query.OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = new List<ReservationDto>(reservations.Count);
        foreach (var reservation in reservations)
        {
            items.Add(await MapAsync(reservation, cancellationToken));
        }

        return new PagedResult<ReservationDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ReservationDto?> GetByIdForStaffAsync(long reservationId, CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var reservation = await CompleteQuery().AsNoTracking().SingleOrDefaultAsync(item => item.Id == reservationId, cancellationToken);
        return reservation is null ? null : await MapAsync(reservation, cancellationToken);
    }

    public async Task<bool> CancelByStaffAsync(
        long reservationId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var reservation = await dbContext.Reservations
            .Include(item => item.AssignedBookCopy)
            .SingleOrDefaultAsync(item => item.Id == reservationId, cancellationToken);
        if (reservation is null)
        {
            return false;
        }
        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Expired or ReservationStatus.Fulfilled)
        {
            return true;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        dbContext.Entry(reservation).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var assignedCopy = reservation.AssignedBookCopy;
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CompletedAtUtc = DateTime.UtcNow;
        reservation.AssignedBookCopy = null;
        reservation.AssignedBookCopyId = null;
        if (assignedCopy is not null)
        {
            await AssignCopyToNextReservationAsync(assignedCopy, cancellationToken);
        }
        AddAudit("ReservationCancelledByStaff", reservation.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkReadyByStaffAsync(long reservationId, string barcode, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        if (string.IsNullOrWhiteSpace(barcode)) throw new ValidationException("An available copy barcode is required.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var reservation = await dbContext.Reservations.Include(item => item.Book)
            .SingleOrDefaultAsync(item => item.Id == reservationId, cancellationToken);
        if (reservation is null) return false;
        if (reservation.Status != ReservationStatus.Waiting) throw new ValidationException("Only a waiting reservation can be marked ready.");
        dbContext.Entry(reservation).Property(item => item.RowVersion).OriginalValue = rowVersion;
        var normalizedBarcode = barcode.Trim();
        var copy = await dbContext.BookCopies.SingleOrDefaultAsync(item => item.Barcode == normalizedBarcode, cancellationToken)
            ?? throw new ValidationException("The scanned copy does not exist.");
        if (copy.BookId != reservation.BookId) throw new ValidationException("The scanned copy is for a different title.");
        if (copy.Status != BookCopyStatus.Available) throw new ValidationException("The scanned copy is not available.");
        var earlierWaiting = await dbContext.Reservations.AnyAsync(item => item.BookId == reservation.BookId && item.Status == ReservationStatus.Waiting &&
            (item.CreatedAtUtc < reservation.CreatedAtUtc || (item.CreatedAtUtc == reservation.CreatedAtUtc && item.Id < reservation.Id)), cancellationToken);
        if (earlierWaiting) throw new ValidationException("An earlier member is ahead in this title's reservation queue.");
        var now = DateTime.UtcNow;
        reservation.Status = ReservationStatus.ReadyForPickup;
        reservation.AssignedBookCopy = copy;
        reservation.AssignedBookCopyId = copy.Id;
        reservation.ReadyAtUtc = now;
        reservation.ExpiresAtUtc = now.AddDays(PickupPeriodDays);
        copy.Status = BookCopyStatus.Reserved;
        copy.UpdatedAtUtc = now;
        AddAudit("ReservationMarkedReadyByStaff", reservation.Id, $"Copy: {copy.Barcode}");
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<int> ExpireReadyReservationsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var expiredReservations = await dbContext.Reservations
            .Include(item => item.AssignedBookCopy)
            .Where(item => item.Status == ReservationStatus.ReadyForPickup &&
                item.ExpiresAtUtc < now)
            .OrderBy(item => item.ExpiresAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var reservation in expiredReservations)
        {
            var assignedCopy = reservation.AssignedBookCopy;
            reservation.Status = ReservationStatus.Expired;
            reservation.CompletedAtUtc = now;
            reservation.AssignedBookCopy = null;
            reservation.AssignedBookCopyId = null;
            if (assignedCopy is not null)
            {
                await AssignCopyToNextReservationAsync(assignedCopy, cancellationToken);
            }
            AddAudit("ReservationExpired", reservation.Id, "Pickup deadline elapsed.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return expiredReservations.Count;
    }

    public async Task AssignCopyToNextReservationAsync(
        BookCopy copy,
        CancellationToken cancellationToken)
    {
        var nextReservation = await dbContext.Reservations
            .Where(item => item.BookId == copy.BookId && item.Status == ReservationStatus.Waiting)
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (nextReservation is null)
        {
            copy.Status = BookCopyStatus.Available;
            copy.UpdatedAtUtc = DateTime.UtcNow;
            return;
        }

        var now = DateTime.UtcNow;
        nextReservation.Status = ReservationStatus.ReadyForPickup;
        nextReservation.AssignedBookCopy = copy;
        nextReservation.AssignedBookCopyId = copy.Id;
        nextReservation.ReadyAtUtc = now;
        nextReservation.ExpiresAtUtc = now.AddDays(PickupPeriodDays);
        copy.Status = BookCopyStatus.Reserved;
        copy.UpdatedAtUtc = now;
    }

    private async Task<LibraryMember> GetCurrentMemberAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            throw new UnauthorizedAccessException("A signed-in library member is required.");
        }

        var member = await dbContext.LibraryMembers.Include(item => item.User).SingleOrDefaultAsync(
            item => item.UserId == currentUser.UserId,
            cancellationToken)
            ?? throw new UnauthorizedAccessException("The signed-in user does not have a library membership.");
        if (!member.IsActive || !member.User.IsActive)
        {
            throw new ValidationException("The library membership is inactive.");
        }
        if (member.MemberType is not LibrarySystem.Domain.Members.MemberType.Student and not LibrarySystem.Domain.Members.MemberType.Teacher)
        {
            throw new UnauthorizedAccessException("Only students and teachers can use member reservations.");
        }

        return member;
    }

    private void EnsureStaff()
    {
        if (!currentUser.IsInRole(RoleNames.Administrator) && !currentUser.IsInRole(RoleNames.Librarian))
            throw new UnauthorizedAccessException("Only authorized library staff can manage reservations.");
    }

    private IQueryable<Reservation> CompleteQuery() => dbContext.Reservations
        .Include(item => item.Book)
        .Include(item => item.Member)
        .Include(item => item.AssignedBookCopy);

    private async Task<ReservationDto> GetReservationDtoAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var reservation = await CompleteQuery().AsNoTracking()
            .SingleAsync(item => item.Id == id, cancellationToken);
        return await MapAsync(reservation, cancellationToken);
    }

    private async Task<ReservationDto> MapAsync(
        Reservation reservation,
        CancellationToken cancellationToken)
    {
        var queuePosition = 0;
        if (reservation.Status == ReservationStatus.Waiting)
        {
            queuePosition = await dbContext.Reservations.CountAsync(
                item => item.BookId == reservation.BookId &&
                    item.Status == ReservationStatus.Waiting &&
                    (item.CreatedAtUtc < reservation.CreatedAtUtc ||
                     (item.CreatedAtUtc == reservation.CreatedAtUtc && item.Id <= reservation.Id)),
                cancellationToken);
        }

        return new ReservationDto
        {
            Id = reservation.Id,
            BookId = reservation.BookId,
            BookTitle = reservation.Book.Title,
            MemberId = reservation.MemberId,
            MemberNumber = reservation.Member.MemberNumber,
            AssignedBookCopyId = reservation.AssignedBookCopyId,
            AssignedBarcode = reservation.AssignedBookCopy?.Barcode,
            Status = reservation.Status,
            QueuePosition = queuePosition,
            CreatedAtUtc = reservation.CreatedAtUtc,
            ExpiresAtUtc = reservation.ExpiresAtUtc,
            RowVersion = reservation.RowVersion
        };
    }

    private void AddAudit(string action, long reservationId, string? details)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = action,
            TargetType = nameof(Reservation),
            TargetId = reservationId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Details = details
        });
    }

    private static LoanDto MapLoan(Loan loan) => new()
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
        Status = loan.Status == LoanStatus.Active && loan.DueAtUtc < DateTime.UtcNow
            ? LoanStatus.Overdue
            : loan.Status,
        RowVersion = loan.RowVersion
    };
}
