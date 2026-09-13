using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Reservations;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Pages.Dashboards;

[Authorize(Policy = PolicyNames.ManageCirculation)]
public sealed class ReservationsModel(IReservationService reservationService, ICirculationService circulationService) : PageModel
{
    [BindProperty(SupportsGet=true), MaxLength(200)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet=true)] public ReservationStatus? Status { get; set; }
    [BindProperty(SupportsGet=true), Range(1,int.MaxValue)] public int PageNumber { get; set; } = 1;
    public PagedResult<ReservationDto> Reservations { get; private set; } = null!;
    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostReadyAsync(long id, string barcode, string rowVersion, CancellationToken cancellationToken) =>
        await ChangeAsync(id, rowVersion, "The reservation is ready for pickup.", token => reservationService.MarkReadyByStaffAsync(id, barcode, token, cancellationToken));

    public async Task<IActionResult> OnPostCancelAsync(long id, string rowVersion, CancellationToken cancellationToken) =>
        await ChangeAsync(id, rowVersion, "The reservation was cancelled.", token => reservationService.CancelByStaffAsync(id, token, cancellationToken));

    public async Task<IActionResult> OnPostFulfillAsync(long id, string rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            var suppliedRowVersion = Convert.FromBase64String(rowVersion);
            var reservation = await reservationService.GetByIdForStaffAsync(id, cancellationToken);
            if (reservation is null) return NotFound();
            if (!reservation.RowVersion.SequenceEqual(suppliedRowVersion)) throw new DbUpdateConcurrencyException();
            if (reservation.Status != ReservationStatus.ReadyForPickup || string.IsNullOrWhiteSpace(reservation.AssignedBarcode))
                throw new ValidationException("Only a ready reservation with an assigned copy can be fulfilled.");
            var loans = await circulationService.CheckoutAsync(new CheckoutRequest { OperationId = Guid.NewGuid(), MemberId = reservation.MemberId, Barcodes = [reservation.AssignedBarcode] }, cancellationToken);
            TempData["StatusMessage"] = $"Reservation #{id} was fulfilled as loan #{loans.Single().Id}.";
        }
        catch (FormatException) { return BadRequest("The reservation concurrency token is invalid."); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "The reservation changed. Review it and try again."; }
        return RedirectToPage(new { SearchTerm, Status, PageNumber });
    }

    private async Task<IActionResult> ChangeAsync(long id, string rowVersion, string message, Func<byte[],Task<bool>> action)
    {
        try { if (!await action(Convert.FromBase64String(rowVersion))) return NotFound(); TempData["StatusMessage"] = message; }
        catch (FormatException) { return BadRequest("The reservation concurrency token is invalid."); }
        catch (ValidationException exception) { TempData["ErrorMessage"] = exception.Message; }
        catch (DbUpdateConcurrencyException) { TempData["ErrorMessage"] = "The reservation changed. Review it and try again."; }
        return RedirectToPage(new { SearchTerm, Status, PageNumber });
    }
    private async Task LoadAsync(CancellationToken cancellationToken) => Reservations = await reservationService.SearchAsync(new ReservationSearchRequest { SearchTerm=SearchTerm, Status=Status, PageNumber=Math.Max(1,PageNumber), PageSize=20 }, cancellationToken);
}
