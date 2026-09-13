using LibrarySystem.Application.Circulation;

namespace LibrarySystem.Application.Reservations;

public sealed class MemberLibraryAccountDto
{
    public required IReadOnlyCollection<LoanDto> ActiveLoans { get; init; }
    public required IReadOnlyCollection<ReservationDto> Reservations { get; init; }
}
