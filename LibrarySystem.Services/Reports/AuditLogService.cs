using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Reports;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Reports;

public sealed class AuditLogService(ApplicationDbContext dbContext) : IAuditLogService
{
    public async Task<AuditLogPageDto> SearchAsync(
        AuditLogRequest request,
        CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
        {
            throw new ValidationException("The start date cannot be after the end date.");
        }

        var query = dbContext.AuditLogs.AsNoTracking().AsQueryable();
        if (request.FromDate.HasValue)
        {
            query = query.Where(log => log.CreatedAtUtc >= request.FromDate.Value.Date);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(log => log.CreatedAtUtc < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(log =>
                log.Action.Contains(searchTerm) ||
                log.TargetType.Contains(searchTerm) ||
                log.TargetId.Contains(searchTerm) ||
                (log.Details != null && log.Details.Contains(searchTerm)) ||
                (log.ActorUserId != null && dbContext.Users.Any(user =>
                    user.Id == log.ActorUserId &&
                    (user.FirstName.Contains(searchTerm) || user.LastName.Contains(searchTerm) ||
                     (user.Email != null && user.Email.Contains(searchTerm))))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await (
                from log in query
                join user in dbContext.Users.AsNoTracking() on log.ActorUserId equals user.Id into actors
                from actor in actors.DefaultIfEmpty()
                orderby log.CreatedAtUtc descending, log.Id descending
                select new AuditLogDto
                {
                    Id = log.Id,
                    ActorUserId = log.ActorUserId,
                    ActorName = actor == null ? "System" : actor.FirstName + " " + actor.LastName,
                    Action = log.Action,
                    TargetType = log.TargetType,
                    TargetId = log.TargetId,
                    Details = log.Details,
                    CreatedAtUtc = log.CreatedAtUtc
                })
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new AuditLogPageDto
        {
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Items = items
        };
    }
}
