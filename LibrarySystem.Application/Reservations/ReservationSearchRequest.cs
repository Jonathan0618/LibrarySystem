using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Reservations;

namespace LibrarySystem.Application.Reservations;

public sealed class ReservationSearchRequest
{
    [MaxLength(200)]
    public string? SearchTerm { get; init; }

    public ReservationStatus? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
