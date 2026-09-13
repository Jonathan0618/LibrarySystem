namespace LibrarySystem.Application.Reports;

public interface IAuditLogService
{
    Task<AuditLogPageDto> SearchAsync(
        AuditLogRequest request,
        CancellationToken cancellationToken = default);
}
