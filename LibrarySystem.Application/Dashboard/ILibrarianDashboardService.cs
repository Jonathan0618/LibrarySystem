namespace LibrarySystem.Application.Dashboard;

public interface ILibrarianDashboardService
{
    Task<LibrarianDashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
