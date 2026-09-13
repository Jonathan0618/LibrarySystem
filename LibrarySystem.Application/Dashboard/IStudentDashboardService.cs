using LibrarySystem.Application.Circulation;

namespace LibrarySystem.Application.Dashboard;

public interface IStudentDashboardService
{
    Task<StudentDashboardDto> GetAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LoanDto>> GetReadingHistoryAsync(
        CancellationToken cancellationToken = default);

    Task<LoanDto?> RenewAsync(
        long loanId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);
}
