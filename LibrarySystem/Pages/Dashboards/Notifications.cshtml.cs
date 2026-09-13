using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Notifications;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Pages.Dashboards;
[Authorize(Policy=PolicyNames.ManageCirculation)]
public sealed class NotificationsModel(INotificationOperationsService operationsService):PageModel
{
    public NotificationOperationsDto Operations {get;private set;}=null!;
    public async Task OnGetAsync(CancellationToken token)=>Operations=await operationsService.GetAsync(token);
    public async Task<IActionResult> OnPostTestAsync(string recipientEmail,NotificationType type,CancellationToken token){try{await operationsService.QueueTestAsync(recipientEmail,type,token);TempData["StatusMessage"]="The test notification was queued.";}catch(ValidationException ex){TempData["ErrorMessage"]=ex.Message;}return RedirectToPage();}
    public async Task<IActionResult> OnPostRetryAsync(long id,CancellationToken token){try{if(!await operationsService.RetryAsync(id,token))return NotFound();TempData["StatusMessage"]="The notification was reset for retry.";}catch(ValidationException ex){TempData["ErrorMessage"]=ex.Message;}return RedirectToPage();}
}
