namespace LibrarySystem.Domain.Reservations;

public enum ReservationStatus
{
    Waiting = 1,
    ReadyForPickup = 2,
    Fulfilled = 3,
    Expired = 4,
    Cancelled = 5
}
