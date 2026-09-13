namespace LibrarySystem.Application.Fines;

public interface IFineService
{
    Task<IReadOnlyCollection<FineDto>> SearchAsync(string? searchTerm, bool outstandingOnly, CancellationToken cancellationToken = default);
    Task<FineDto?> GetAsync(long fineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FineDto>> GetForMemberAsync(long memberId, CancellationToken cancellationToken = default);
    Task<FineDto?> RecordPaymentAsync(long fineId, FineActionRequest request, CancellationToken cancellationToken = default);
    Task<FineDto?> AdjustAsync(long fineId, FineActionRequest request, CancellationToken cancellationToken = default);
    Task<FineDto?> WaiveAsync(long fineId, FineActionRequest request, CancellationToken cancellationToken = default);
}
