using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Fines;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Fines;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Fines;

public sealed class FineService(ApplicationDbContext dbContext, ICurrentUser currentUser) : IFineService
{
    public async Task<IReadOnlyCollection<FineDto>> SearchAsync(string? searchTerm, bool outstandingOnly, CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var query = CompleteQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(item => item.Member.MemberNumber.Contains(term) || item.Member.User.FirstName.Contains(term) || item.Member.User.LastName.Contains(term) || item.Reason.Contains(term));
        }
        if (outstandingOnly) query = query.Where(item => item.Balance > 0 && (item.Status == FineStatus.Outstanding || item.Status == FineStatus.PartiallyPaid));
        return (await query.OrderByDescending(item => item.CreatedAtUtc).Take(500).ToListAsync(cancellationToken)).Select(Map).ToArray();
    }

    public async Task<FineDto?> GetAsync(long fineId, CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var fine = await CompleteQuery().AsNoTracking().SingleOrDefaultAsync(item => item.Id == fineId, cancellationToken);
        return fine is null ? null : Map(fine);
    }

    public async Task<IReadOnlyCollection<FineDto>> GetForMemberAsync(long memberId, CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        return (await CompleteQuery().AsNoTracking().Where(item => item.MemberId == memberId)
            .OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken)).Select(Map).ToArray();
    }

    public Task<FineDto?> RecordPaymentAsync(long fineId, FineActionRequest request, CancellationToken cancellationToken = default) =>
        ApplyAsync(fineId, request, FineTransactionType.Payment, cancellationToken);

    public Task<FineDto?> AdjustAsync(long fineId, FineActionRequest request, CancellationToken cancellationToken = default) =>
        ApplyAsync(fineId, request, FineTransactionType.Adjustment, cancellationToken);

    public async Task<FineDto?> WaiveAsync(long fineId, FineActionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var fine = await CompleteQuery().SingleOrDefaultAsync(item => item.Id == fineId, cancellationToken);
        if (fine is null) return null;
        ValidateReason(request.Reason);
        ValidateConcurrencyToken(request.RowVersion);
        EnsureActionable(fine);
        dbContext.Entry(fine).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        var amount = fine.Balance;
        fine.Balance = 0;
        fine.Status = FineStatus.Waived;
        fine.UpdatedAtUtc = DateTime.UtcNow;
        fine.Transactions.Add(CreateTransaction(fine, FineTransactionType.Waiver, amount, request.Reason));
        AddAudit(fine, FineTransactionType.Waiver, amount);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(fine);
    }

    private async Task<FineDto?> ApplyAsync(long fineId, FineActionRequest request, FineTransactionType type, CancellationToken cancellationToken)
    {
        EnsureStaff();
        var fine = await CompleteQuery().SingleOrDefaultAsync(item => item.Id == fineId, cancellationToken);
        if (fine is null) return null;
        ValidateReason(request.Reason);
        ValidateConcurrencyToken(request.RowVersion);
        EnsureActionable(fine);
        if (request.Amount != decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero))
            throw new ValidationException("The amount cannot contain more than two decimal places.");
        if (request.Amount <= 0 || request.Amount > fine.Balance) throw new ValidationException("The amount must be greater than zero and cannot exceed the balance.");
        dbContext.Entry(fine).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        fine.Balance -= request.Amount;
        fine.Status = fine.Balance == 0 ? FineStatus.Paid : FineStatus.PartiallyPaid;
        fine.UpdatedAtUtc = DateTime.UtcNow;
        fine.Transactions.Add(CreateTransaction(fine, type, request.Amount, request.Reason));
        AddAudit(fine, type, request.Amount);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(fine);
    }

    private FineTransaction CreateTransaction(Fine fine, FineTransactionType type, decimal amount, string reason) => new()
    {
        Fine = fine, Type = type, Amount = amount, Reason = reason.Trim(), ActorUserId = currentUser.UserId
    };

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new ValidationException("A reason of 500 characters or fewer is required.");
    }

    private static void ValidateConcurrencyToken(byte[] rowVersion)
    {
        if (rowVersion.Length == 0)
            throw new ValidationException("A valid concurrency token is required.");
    }

    private static void EnsureActionable(Fine fine)
    {
        if (fine.Status is FineStatus.Paid or FineStatus.Waived || fine.Balance <= 0)
            throw new ValidationException("A paid or waived fine cannot be changed.");
    }

    private void AddAudit(Fine fine, FineTransactionType type, decimal amount) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = $"Fine{type}",
            TargetType = nameof(Fine),
            TargetId = fine.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Details = $"Amount: {amount:F2}"
        });

    private static FineDto Map(Fine fine) => new()
    {
        Id = fine.Id, MemberId = fine.MemberId, MemberNumber = fine.Member.MemberNumber, LoanId = fine.LoanId,
        Type = fine.Type, AssessedAmount = fine.AssessedAmount, Balance = fine.Balance, Status = fine.Status,
        Reason = fine.Reason, CreatedAtUtc = fine.CreatedAtUtc, RowVersion = fine.RowVersion,
        MemberName = $"{fine.Member.User.FirstName} {fine.Member.User.LastName}",
        Transactions = fine.Transactions.OrderByDescending(item => item.CreatedAtUtc).Select(item => new FineTransactionDto { Id = item.Id, Type = item.Type, Amount = item.Amount, Reason = item.Reason, ActorUserId = item.ActorUserId, CreatedAtUtc = item.CreatedAtUtc }).ToArray()
    };

    private IQueryable<Fine> CompleteQuery() => dbContext.Fines.Include(item => item.Member).ThenInclude(item => item.User).Include(item => item.Transactions);

    private void EnsureStaff()
    {
        if (!currentUser.IsInRole(RoleNames.Administrator) && !currentUser.IsInRole(RoleNames.Librarian)) throw new UnauthorizedAccessException("Only authorized staff can manage fines.");
    }
}
