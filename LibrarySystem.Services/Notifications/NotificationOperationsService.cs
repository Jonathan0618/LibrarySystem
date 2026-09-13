using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Notifications;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Notifications;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LibrarySystem.Services.Notifications;

public sealed class NotificationOperationsService(ApplicationDbContext dbContext, ICurrentUser currentUser, IConfiguration configuration, IHostEnvironment environment) : INotificationOperationsService
{
    public async Task<NotificationOperationsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var counts = await dbContext.QueuedNotifications.AsNoTracking().GroupBy(item => item.Status).Select(group => new { group.Key, Count=group.Count() }).ToDictionaryAsync(item => item.Key,item => item.Count,cancellationToken);
        var recent = await dbContext.QueuedNotifications.AsNoTracking().OrderByDescending(item=>item.CreatedAtUtc).Take(100).Select(item=>new NotificationQueueItemDto { Id=item.Id,Type=item.Type,RecipientEmail=item.RecipientEmail,Subject=item.Subject,Status=item.Status,AttemptCount=item.AttemptCount,NextAttemptAtUtc=item.NextAttemptAtUtc,SentAtUtc=item.SentAtUtc,LastError=item.LastError }).ToArrayAsync(cancellationToken);
        var host=configuration["Notifications:Smtp:Host"]; var from=configuration["Notifications:Smtp:FromAddress"]; var name=configuration["Notifications:Smtp:FromName"];
        return new NotificationOperationsDto { UsesProductionSmtp=!environment.IsDevelopment(), SmtpConfigured=environment.IsDevelopment() || (!string.IsNullOrWhiteSpace(host)&&!string.IsNullOrWhiteSpace(from)&&!string.IsNullOrWhiteSpace(configuration["Notifications:Smtp:Password"])), SenderIdentity=environment.IsDevelopment()?"Development notification capture":$"{name} <{from}>", PendingCount=counts.GetValueOrDefault(NotificationStatus.Pending),ProcessingCount=counts.GetValueOrDefault(NotificationStatus.Processing),FailedCount=counts.GetValueOrDefault(NotificationStatus.Failed),SentCount=counts.GetValueOrDefault(NotificationStatus.Sent),RecentItems=recent };
    }
    public async Task QueueTestAsync(string recipientEmail, NotificationType type, CancellationToken cancellationToken=default)
    {
        EnsureStaff();
        if(!new EmailAddressAttribute().IsValid(recipientEmail)) throw new ValidationException("A valid recipient email is required.");
        var (subject,body)=type switch { NotificationType.DueSoon=>("School Library: book due soon","This is a delivery test for due-soon reminders. Sign in to your library account to review your loans."), NotificationType.Overdue=>("School Library: overdue notice","This is a delivery test for overdue notices. Sign in to your library account or contact library staff."), NotificationType.ReadyForPickup=>("School Library: reservation ready","This is a delivery test for reservation pickup notices. Sign in to review the pickup deadline."), _=>("School Library: account notification","This is a delivery test for account-security notifications. Library staff will never ask for your password.") };
        dbContext.QueuedNotifications.Add(new QueuedNotification { DeduplicationKey=$"delivery-test:{Guid.NewGuid():N}",Type=type,RecipientEmail=recipientEmail.Trim(),Subject=subject,Body=body });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task<bool> RetryAsync(long id,CancellationToken cancellationToken=default)
    {
        EnsureStaff(); var item=await dbContext.QueuedNotifications.SingleOrDefaultAsync(x=>x.Id==id,cancellationToken); if(item is null)return false;
        if(item.Status==NotificationStatus.Sent)throw new ValidationException("A delivered notification cannot be retried.");
        item.Status=NotificationStatus.Pending;item.AttemptCount=0;item.NextAttemptAtUtc=DateTime.UtcNow;item.LastError=null;await dbContext.SaveChangesAsync(cancellationToken);return true;
    }
    private void EnsureStaff(){if(!currentUser.IsInRole(RoleNames.Administrator)&&!currentUser.IsInRole(RoleNames.Librarian))throw new UnauthorizedAccessException("Only authorized staff can manage notifications.");}
}
