namespace LibrarySystem.Application.Circulation;

public interface ISchoolCalendarService
{
    Task<IReadOnlyCollection<LibraryPolicyDto>> GetPoliciesAsync(CancellationToken cancellationToken = default);
    Task<LibraryPolicyDto?> UpdatePolicyAsync(int id, UpdateLibraryPolicyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SchoolHolidayDto>> GetHolidaysAsync(
        int year,
        CancellationToken cancellationToken = default);

    Task<SchoolHolidayDto> SaveHolidayAsync(
        SchoolHolidayRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteHolidayAsync(
        int id,
        CancellationToken cancellationToken = default);
}
