using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Reservations;
using LibrarySystem.Domain.Fines;
using LibrarySystem.Domain.Notifications;

namespace LibrarySystem.Application.Dashboard;

public sealed class StudentDashboardDto
{
    public required string StudentName { get; init; }
    public required string MemberNumber { get; init; }
    public string? Grade { get; init; }
    public required IReadOnlyCollection<LoanDto> ActiveLoans { get; init; }
    public required IReadOnlyCollection<ReservationDto> Reservations { get; init; }
    public required IReadOnlyCollection<StudentDashboardBookDto> NewArrivals { get; init; }
    public int BooksReadThisYear { get; init; }
    public decimal OutstandingFineBalance { get; init; }
    public decimal CheckoutBalanceLimit { get; init; }
    public bool CheckoutBlocked { get; init; }
    public required IReadOnlyCollection<StudentFineDto> Fines { get; init; }
    public required IReadOnlyCollection<StudentActivityDto> Activity { get; init; }
}

public sealed class StudentFineDto
{
    public long Id { get; init; }
    public FineType Type { get; init; }
    public FineStatus Status { get; init; }
    public decimal Balance { get; init; }
    public required string Reason { get; init; }
}

public sealed class StudentActivityDto
{
    public NotificationType Type { get; init; }
    public NotificationStatus DeliveryStatus { get; init; }
    public required string Subject { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

public sealed class StudentDashboardBookDto
{
    public long Id { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
}
