using LibrarySystem.Application.Common;

namespace LibrarySystem.Application.Reservations;

public interface IReservationService
{
    Task<ReservationDto> PlaceForCurrentMemberAsync(
        long bookId,
        CancellationToken cancellationToken = default);

    Task<bool> CancelForCurrentMemberAsync(
        long reservationId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReservationDto>> GetForCurrentMemberAsync(
        CancellationToken cancellationToken = default);

    Task<MemberLibraryAccountDto> GetLibraryAccountForCurrentMemberAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationDto>> SearchAsync(
        ReservationSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationDto?> GetByIdForStaffAsync(long reservationId, CancellationToken cancellationToken = default);

    Task<bool> CancelByStaffAsync(
        long reservationId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<bool> MarkReadyByStaffAsync(
        long reservationId,
        string barcode,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<int> ExpireReadyReservationsAsync(CancellationToken cancellationToken = default);
}
